// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.RegularExpressions;
using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging;
using OSRobot.Server.Core.Logging.Abstract;
using OSRobot.Server.Core.Persistence;
using OSRobot.Server.JobEngineLib.Infrastructure.Abstract;


namespace OSRobot.Server.JobEngineLib;

public partial class JobEngine(IAppLogger appLogger, IJobEngineConfig config) : IJobEngine, IEventSink
{
    private readonly IAppLogger _log = appLogger;
    private readonly IJobEngineConfig _config = config;
    private IFolder _rootFolder = new Folder();

    private List<IEvent> _events = [];
    private List<ITask> _tasks = [];

    // Log cleanup timer. Created in Start(), fully torn down in Stop().
    private System.Timers.Timer? _logCleanTimer;
    private int _logCleanInProgress;   // 0 = idle, 1 = running

    // Log name pattern
    private readonly Regex _logNameRegex = LogNameRegex();

    // Serializes concurrent ReloadJobs() calls.
    private readonly object _reloadLock = new();

    // Lifecycle gate. Guards _queue: it is non-null exactly while the engine accepts new runs
    // (from events or manual starts). Start() creates it, Stop() takes it away before stopping it,
    // so a run is only ever created on a queue that has not been stopped yet.
    private readonly object _lifecycleGate = new();
    private TaskQueue? _queue;             // guarded by _lifecycleGate

    // Monotonic id source for runs, used to correlate log lines. Never reset.
    private long _runIdSeq;

    /// <returns>null if the engine is not accepting work (stopped, stopping or reloading).</returns>
    private JobRun? TryBeginRun(CancellationToken callerToken = default)
    {
        lock (_lifecycleGate)
        {
            if (_queue == null)
                return null;

            return new JobRun(Interlocked.Increment(ref _runIdSeq), _queue, callerToken);
        }
    }

    private int GetWorkerCount()
    {
        if (_config.SerialExecution)
            return 1;

        return _config.MaxConcurrentTasks > 0 ? _config.MaxConcurrentTasks : Constants.DefaultMaxConcurrentTasks;
    }

    private bool IsValidLogName(string logName)
    {
        return _logNameRegex.IsMatch(logName);
    }

    private bool LoadJobData()
    {
        bool result = true;
        string dataPath = _config.DataPath;
        _log.Info($"Loading jobs data from directory: {dataPath}");

        try
        {
            _log.Info("Loading jobs data...");
            IFolder? folder = JobsPersistence.LoadJobEditorJSON(dataPath, "jobs.json");
            if (folder == null)
            {
                _log.Error("Error loading data");
                result = false;
            }
            else
            {
                _rootFolder = folder;

                foreach (DynamicDataIssue issue in DynamicDataValidator.Validate(folder))
                    _log.Warn($"Dynamic data reference problem: {issue}");
            }
        }
        catch (Exception ex)
        {
            _log.Error("Error loading data", ex);
            result = false;
        }

        return result;
    }

    private List<IEvent> GetEventList(IFolder folder)
    {
        List<IEvent> events = [];

        foreach (IPluginInstanceBase pluginInstance in folder)
        {
            if (pluginInstance is IEvent @event)
            {
                // Skip disabled events
                if (!pluginInstance.Config.Enabled)
                    continue;

                events.Add(@event);
            }
            else if (pluginInstance is IFolder innerFolder)
            {
                List<IEvent> innerFolderEvents = GetEventList(innerFolder);
                events.AddRange(innerFolderEvents);
            }
        }

        return events;
    }

    private List<ITask> GetTaskList(IFolder folder)
    {
        List<ITask> tasks = [];

        foreach (IPluginInstanceBase pluginInstance in folder)
        {
            if (pluginInstance is ITask innerTask)
            {
                tasks.Add(innerTask);
            }
            else if (pluginInstance is IFolder innerFolder)
            {
                List<ITask> innerFolderTasks = GetTaskList(innerFolder);
                tasks.AddRange(innerFolderTasks);
            }
        }

        return tasks;
    }

    private IFolder? FindFolderRecursive(IFolder folder, int folderId)
    {
        if (folder.Config.Id == folderId)
            return folder;

        foreach (IPluginInstanceBase pluginInstanceBase in folder.Items)
        {
            if (pluginInstanceBase is IFolder innerFolder)
            {
                if (pluginInstanceBase.Config.Id == folderId)
                    return innerFolder;
                else
                {
                    IFolder? folderFound = FindFolderRecursive(innerFolder, folderId);
                    if (folderFound != null)
                        return folderFound;
                }
            }
        }

        return null;
    }

    private LogInfo CreateLogInfoItemFromLogName(int folderId, string logName)
    {
        if (string.IsNullOrEmpty(logName))
            throw new ApplicationException("_logInfoItemFromLogName: input param 'logName' is empty");

        Match match = _logNameRegex.Match(logName);
        
        if (!match.Success)
            throw new ApplicationException("_logInfoItemFromLogName: invalid string format");

        int eventId = int.Parse(match.Groups[1].Value);

        if (!DateTime.TryParse(match.Groups[2].Value.Replace('_', ':'), out DateTime execDateTime))
            throw new ApplicationException("_logInfoItemFromLogName: date/time string format");

        return new LogInfo() { FolderId = folderId, EventId = eventId, ExecDateTime = execDateTime, FileName = logName };
    }

    // IEventSink: called by an event, on its own thread (a Timer/FileSystemWatcher callback, typically),
    // each time it occurs. Only evaluates the event's connections and queues the tasks to run, so the
    // event source's thread is never held by task execution or by a connection's WaitSeconds.
    bool IEventSink.Publish(IEvent source, DynamicDataSet dynamicData, IPluginInstanceLogger logger)
    {
        JobRun? run = TryBeginRun();
        if (run == null)
        {
            _log.Info($"Event from object {source.Config.Id} ignored: engine is not accepting events.");
            return false;
        }

        try
        {
            logger.EventTriggered(source);
            _log.Info($"Event triggered by object: {source.Config.Id}:{source.Config.Name}:{source.GetType().Name} (run {run.Id})");

            DispatchSuccessors(run, source, [new ExecResult(true, dynamicData)], [], isEvent: true, logger);
        }
        catch (Exception ex)
        {
            _log.Error($"Unhandled error dispatching event {source.Config.Id}", ex);
        }
        finally
        {
            run.CompleteItem();
        }

        return true;
    }

    /// <summary>
    /// Evaluates the source's outgoing connections against each of its results and queues the target
    /// task once per result that satisfies the connection's conditions.
    /// </summary>
    private void DispatchSuccessors(JobRun run, IPluginInstance source, List<ExecResult> execResults, DynamicDataChain dataChain,
                                    bool isEvent, IPluginInstanceLogger logger)
    {
        foreach (PluginInstanceConnection connection in source.Connections)
        {
            if (!connection.Enabled)
                continue;

            ITask target = (ITask)connection.ConnectTo;

            if (!target.Config.Enabled)
            {
                _log.Info($"Task: {target.Config.Id}:{target.Config.Name}:{target.GetType().Name} disabled, skipped");
                continue;
            }

            // A failure evaluating one connection (e.g. a condition on a missing dynamic data value)
            // must not stop the sibling connections from being dispatched.
            try
            {
                for (int i = 0; i < execResults.Count; i++)
                {
                    ExecResult execResult = execResults[i];
                    if (!connection.EvaluateExecConditions(execResult))
                        continue;

                    // Each target gets its own copy of the chain, extended with the result it runs for.
                    DynamicDataChain dataChainCopy = dataChain.Clone();
                    dataChainCopy.TryAdd(source.Config.Id, execResult.Data);

                    // Events have no iterations: a task started by an event has no sub-instance index.
                    int? subInstanceIndex = isEvent ? null : i;
                    Schedule(new TaskWorkItem(run, target, dataChainCopy, execResult.Data, subInstanceIndex, logger), connection.WaitSeconds);
                }
            }
            catch (Exception ex)
            {
                logger.Error(source, $"Error dispatching connection to object {target.Config.Id}", ex);
            }
        }
    }

    /// <summary>Queues a task of the run, after waitSeconds if given. The wait holds a timer, not a worker.</summary>
    private static void Schedule(TaskWorkItem item, int? waitSeconds)
    {
        item.Run.AddItem();

        if (waitSeconds is > 0)
            _ = EnqueueAfterDelayAsync(item, waitSeconds.Value);
        else
            Enqueue(item);
    }

    private static async Task EnqueueAfterDelayAsync(TaskWorkItem item, int waitSeconds)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(waitSeconds), item.Run.Token);
        }
        catch (OperationCanceledException)
        {
            // Engine stopping: the task is dropped without running.
            item.Run.CompleteItem();
            return;
        }

        Enqueue(item);
    }

    private static void Enqueue(TaskWorkItem item)
    {
        // Refused only once Stop() has stopped the run's queue: dropping the task is exactly what Stop() wants.
        if (!item.Run.Queue.TryEnqueue(item))
            item.Run.CompleteItem();
    }

    /// <summary>Runs on a queue worker: executes one task, then queues its successors.</summary>
    private async Task ProcessWorkItemAsync(TaskWorkItem item)
    {
        JobRun run = item.Run;

        try
        {
            // Stopping, or the caller gave up: drain the queue without running anything.
            if (run.Token.IsCancellationRequested)
                return;

            ITask? taskCopy = null;
            InstanceExecResult instExecResult;
            try
            {
                // Run a clone, never the shared definition: the same task can run several times at once.
                taskCopy = (ITask?)CoreHelpers.CloneObjects(item.Task);
                if (taskCopy == null)
                    throw new ApplicationException("Cloning configuration returned null");

                if (taskCopy.Config.Log)
                    item.Logger.TaskStarting(taskCopy);

                item.Logger.Info($"About to run task {taskCopy.Config.Id} (run {run.Id})");
                instExecResult = await taskCopy.RunAsync(item.DataChain, item.LastDynamicDataSet, item.SubInstanceIndex, item.Logger, run.Token);

                if (taskCopy.Config.Log)
                    item.Logger.TaskEnded(taskCopy);
            }
            catch (OperationCanceledException) when (run.Token.IsCancellationRequested)
            {
                item.Logger.Info($"Task {item.Task.Config.Id} (run {run.Id}) cancelled: engine is stopping or the request was aborted.");
                return;
            }
            catch (Exception ex)
            {
                if (taskCopy != null)
                    item.Logger.Error(taskCopy, "ExecuteTask", ex);
                else
                    item.Logger.Error("ExecuteTask: TaskCopy object is null.", ex);
                return;
            }

            // Successors come from the definition, not the clone: the graph is the same and it doesn't
            // depend on how deep the clone went.
            DispatchSuccessors(run, item.Task, instExecResult.ExecResults, item.DataChain, isEvent: false, item.Logger);
        }
        finally
        {
            run.CompleteItem();
        }
    }

    private bool IsDirectoryEmpty(string directoryPath)
    {
        return (Directory.GetFiles(directoryPath).Length == 0 && Directory.GetDirectories(directoryPath).Length == 0);
    }

    private void CleanUpLog(string logPath, int cleanUpLogsOlderThanHours)
    {
        DateTime dateLimit = DateTime.Now.AddHours(-cleanUpLogsOlderThanHours);
        string[] files = Directory.GetFiles(logPath);
        foreach (string fullPathFileName in files)
        {
            FileInfo fi = new(fullPathFileName);
            if (fi.CreationTime < dateLimit)
                fi.Delete();
        }

        string[] directories = Directory.GetDirectories(logPath);
        foreach (string fullPathDirectoryName in directories)
        {
            CleanUpLog(fullPathDirectoryName, cleanUpLogsOlderThanHours);
            if (IsDirectoryEmpty(fullPathDirectoryName))
                Directory.Delete(fullPathDirectoryName);
        }
    }

    private void LogCleanTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        // Skip if a previous cleanup is still running (long CleanUpLog + short interval).
        if (Interlocked.CompareExchange(ref _logCleanInProgress, 1, 0) != 0)
            return;

        try
        {
            _log.Info("Cleaning up old logs");
            CleanUpLog(_config.LogPath, _config.CleanUpLogsOlderThanHours);
        }
        catch (Exception ex)
        {
            _log.Error("An error occurred while cleaning up old logs", ex);
        }
        finally
        {
            Interlocked.Exchange(ref _logCleanInProgress, 0);
        }
    }

    public void Start(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            _log.Info("Start cancelled before JobEngine initialization began.");
            return;
        }

        try
        {
            Server.Core.Core.Init(_config.LogPath, _config.ScriptingEnabled, _config.RunProgramAllowedExecutablePaths);

            _log.Info("Starting OSRobot.JobEngine...");

            _log.Info("Loading job data");
            if (!LoadJobData())
            {
                _log.Info("Error loading job data, JobEngine is not working...");
                return;
            }

            if (_rootFolder == null)
            {
                _log.Info("There are no jobs to load...");
                return;
            }

            // Set the timer to clean logs
            if (_config.CleanUpLogsOlderThanHours > 0)
            {
                try
                {
                    // Trigger a clean up at service startup
                    _log.Info("Cleaning up old logs on initialization");
                    CleanUpLog(_config.LogPath, _config.CleanUpLogsOlderThanHours);
                }
                catch (Exception ex)
                {
                    _log.Error("An error occurred while cleaning up old logs on initialization", ex);
                }

                _logCleanTimer = new()
                {
                    Interval = new TimeSpan(0, _config.CleanUpLogsIntervalHours, 0, 0).TotalMilliseconds,
                    AutoReset = true
                };
                _logCleanTimer.Elapsed += LogCleanTimer_Elapsed;
                _logCleanTimer.Start();
            }

            // Initializes tasks first, then initializes events
            // This guarantee that events will not trigger untils all tasks are initialized
            _log.Info("Starting tasks initialization");
            _tasks = GetTaskList(_rootFolder);
            _tasks.ForEach(t =>
            {
                _log.Info($"Initializing task: {t.Config.Id}:{t.Config.Name}:{t.GetType().Name}");
                t.Init();
            });

            // From here on runs are accepted. This must happen before events are initialized: an event
            // may publish from inside Init() (OSRobotServiceStartEvent does), and that must not be ignored.
            int workerCount = GetWorkerCount();
            _log.Info($"Starting task queue with {workerCount} worker(s)");
            lock (_lifecycleGate)
            {
                _queue = new TaskQueue(workerCount, ProcessWorkItemAsync, _log);
            }

            _log.Info("Starting events initialization");
            _events = GetEventList(_rootFolder);
            _events.ForEach(t =>
            {
                _log.Info($"Initializing event: {t.Config.Id}:{t.Config.Name}:{t.GetType().Name}");
                t.Init(this);
            });
        }
        catch (Exception ex)
        {
            _log.Error("An error occurred while starting JobEngine.", ex);
        }
    }

    public void Stop(CancellationToken cancellationToken = default)
    {
        // Bounds how long Stop() waits for the work in flight to finish before proceeding with
        // teardown anyway. Configurable via AppSettings:JobEngineConfig:StopDrainTimeoutSeconds.
        TimeSpan drainTimeout = TimeSpan.FromSeconds(Math.Max(0, _config.StopDrainTimeoutSeconds));

        try
        {
            // Stop accepting new runs. A run created just before this keeps the queue it was created
            // on, which is stopped right after: whatever it still tries to enqueue is refused or skipped.
            TaskQueue? queue;
            lock (_lifecycleGate)
            {
                queue = _queue;
                _queue = null;
            }

            // Ask everything in flight to stop (a mid-HTTP-call, a connection's WaitSeconds, ...) and
            // give it a bounded window to do so before destroying the task instances. The wait also
            // honors cancellationToken, so a host shutdown with a short HostOptions.ShutdownTimeout
            // can cut it short.
            if (queue != null && !queue.Stop(drainTimeout, cancellationToken))
            {
                _log.Warn($"{queue.PendingCount} task(s) still in flight after waiting to stop " +
                          $"({(cancellationToken.IsCancellationRequested ? "shutdown cancelled" : "timeout")}); proceeding with teardown anyway.");
            }

            // Fully tear down the log cleanup timer (stop, unsubscribe, dispose).
            if (_logCleanTimer is not null)
            {
                _logCleanTimer.Stop();
                _logCleanTimer.Elapsed -= LogCleanTimer_Elapsed;
                _logCleanTimer.Dispose();
                _logCleanTimer = null;
            }

            _log.Info("Destroying events");
            _events.ForEach(E =>
            {
                _log.Info($"Destroying event: {E.Config.Id}:{E.Config.Name}:{E.GetType().Name}");
                E.Destroy();
            });
            _events = [];

            _log.Info("Destroying tasks");
            _tasks.ForEach(T =>
            {
                _log.Info($"Destroying task: {T.Config.Id}:{T.Config.Name}:{T.GetType().Name}");
                T.Destroy();
            });
            _tasks = [];
        }
        catch (Exception ex)
        {
            _log.Error("An error occurred while stopping JobEngine.", ex);
        }
    }

    public async Task<bool> StartTaskAsync(int taskID, CancellationToken cancellationToken = default)
    {
        _log.Info($"Requested execution of task: {taskID}");

        ITask? taskObj = _tasks.Where(task => task.Config.Id == taskID).FirstOrDefault();
        if (taskObj == null)
        {
            _log.Info($"The task {taskID} cannot be found.");
            return false;
        }

        // In serial mode this call only returns once the run has finished, so the caller is still
        // waiting on it: "the caller gave up" (e.g. the HTTP request was aborted) must cancel the run,
        // as well as "the engine is stopping".
        //
        // Otherwise this call returns as soon as the task is queued and the run continues in the
        // background. It must then follow only the engine: a caller token such as an HTTP request's
        // RequestAborted must not outlive the request that is already being answered.
        JobRun? run = TryBeginRun(_config.SerialExecution ? cancellationToken : CancellationToken.None);
        if (run == null)
        {
            _log.Info($"Cannot start task {taskID}: the engine is not accepting new work (stopping/reloading).");
            return false;
        }

        try
        {
            DateTime now = DateTime.Now;
            IPluginInstanceLogger logger = PluginInstanceLogger.GetLogger(taskObj);
            DynamicDataSet dDataSet = CommonDynamicData.BuildStandardDynamicDataSet(taskObj, true, 0, now, now, 1);

            _log.Info($"Queueing task {taskID} (run {run.Id})");
            Schedule(new TaskWorkItem(run, taskObj, [], dDataSet, null, logger), waitSeconds: null);
        }
        catch (Exception ex)
        {
            _log.Error("An error occurred while executing the requested task.", ex);
            return false;
        }
        finally
        {
            run.CompleteItem();
        }

        if (_config.SerialExecution)
            await run.Completion;

        return true;
    }

    public ReloadJobsReturnValues ReloadJobs()
    {
        // Serialize concurrent reload requests - two overlapping Stop()/Start()
        // pairs would otherwise interleave and leave the engine in an inconsistent state.
        lock (_reloadLock)
        {
            _log.Info("Trying to reload job data...");

            // This is a cheap up-front check for a friendlier caller response; it can be
            // stale (a run can start right after it). Safety doesn't depend on it: Stop()
            // stops accepting runs and cancels/drains the queue before it tears anything down.
            // Tasks waiting out a connection's WaitSeconds are not counted: they are not
            // running yet, and a reload must not be held off for the length of a long wait.
            int pendingTasks;
            lock (_lifecycleGate)
            {
                pendingTasks = _queue?.PendingCount ?? 0;
            }

            if (pendingTasks > 0)
            {
                _log.Info($"Cannot reload jobs now, there are {pendingTasks} queued or running tasks, please retry later.");
                return ReloadJobsReturnValues.CannotReloadWhileRunningTask;
            }

            _log.Info("Stopping and restarting JobEngine to reload jobs...");
            Stop();
            Start();

            return ReloadJobsReturnValues.Ok;
        }
    }

    public List<IPlugin> GetPlugins() => PluginRegistry.GetPlugins();

    public IPlugin? GetPlugin(string pluginId) => PluginRegistry.GetPlugin(pluginId);

    public List<LogInfo> GetFolderLogs(int folderId)
    {
        List<LogInfo> folderLogs = [];
        IFolder? folder = FindFolderRecursive(_rootFolder, folderId);
        if (folder == null)
            return folderLogs;

        string logFullPath = Path.Combine(_config.LogPath, folder.GetPhysicalFullPath());

        if (Directory.Exists(logFullPath))
        {
            // Return logfiles containted in the specified folder.
            // Check that the filename matches the expected pattern.
            string[] logFiles = Directory.GetFiles(logFullPath);
            folderLogs.AddRange(
                logFiles.Where(file => IsValidLogName(Path.GetFileName(file)))
                        .Select(file => CreateLogInfoItemFromLogName(folderId, Path.GetFileName(file)))
            );
        }

        return folderLogs;
    }

    public FolderInfo? GetFolderInfo(int folderId)
    {
        IFolder? folder = FindFolderRecursive(_rootFolder, folderId);
        if (folder == null)
            return null;

        string logFullPath = folder.GetPhysicalFullPath();

        return new FolderInfo() {   Id = folderId, 
                                    Name = folder.Config.Name,
                                    LogPath = logFullPath
        };
    }

    public string? GetLogContent(int folderId, string logFileName)
    {
        IFolder? folder = FindFolderRecursive(_rootFolder, folderId);
        if (folder == null || !IsValidLogName(logFileName))
            return null;

        string logFullPath = Path.Combine(_config.LogPath, folder.GetPhysicalFullPath(), logFileName);

        if (!File.Exists(logFullPath))
            return string.Empty;

        return File.ReadAllText(logFullPath);
    }

    [GeneratedRegex(@"^(\d+)_(\d{4}-\d{2}-\d{2}T\d{2}_\d{2}_\d{2})_(\d+)_(\d+).log$")]
    private static partial Regex LogNameRegex();
}
