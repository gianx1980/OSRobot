/*======================================================================================
    Copyright 2025 by Gianluca Di Bucci (gianx1980) (https://www.os-robot.com)

    This file is part of OSRobot.

    OSRobot is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    OSRobot is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with OSRobot.  If not, see <http://www.gnu.org/licenses/>.
======================================================================================*/
using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging;
using OSRobot.Server.Core.Logging.Abstract;
using OSRobot.Server.Core.Persistence;
using OSRobot.Server.JobEngineLib.Infrastructure.Abstract;
using OSRobot.Server.Plugins.CpuEvent;
using OSRobot.Server.Plugins.DateTimeEvent;
using OSRobot.Server.Plugins.DiskSpaceEvent;
using OSRobot.Server.Plugins.ExcelFileTask;
using OSRobot.Server.Plugins.FileSystemEvent;
using OSRobot.Server.Plugins.FileSystemTask;
using OSRobot.Server.Plugins.FtpSftpTask;
using OSRobot.Server.Plugins.MemoryEvent;
using OSRobot.Server.Plugins.OSRobotServiceStartEvent;
using OSRobot.Server.Plugins.ReadTextFileTask;
using OSRobot.Server.Plugins.RESTApiTask;
using OSRobot.Server.Plugins.RunProgramTask;
using OSRobot.Server.Plugins.SendEMailTask;
using OSRobot.Server.Plugins.SqlServerBackupTask;
using OSRobot.Server.Plugins.SqlServerBulkCopyTask;
using OSRobot.Server.Plugins.SqlServerCommandTask;
using OSRobot.Server.Plugins.SystemEventsEvent;
using OSRobot.Server.Plugins.UnzipTask;
using OSRobot.Server.Plugins.WriteTextFileTask;
using OSRobot.Server.Plugins.ZipTask;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace OSRobot.Server.JobEngineLib;

public partial class JobEngine(IAppLogger appLogger, IJobEngineConfig config) : IJobEngine
{
    private readonly IAppLogger _log = appLogger;
    private readonly IJobEngineConfig _config = config;
    private IFolder _rootFolder = new Folder();

    private List<IEvent> _events = [];
    private List<ITask> _tasks = [];
    private System.Timers.Timer _logCleanTimer = new();

    // Log name pattern
    private readonly Regex _logNameRegex = LogNameRegex();

    // Keep track of running tasks
    private long _runningTasksCount;
    private readonly object _lockRunningTasksCount = new();
    private readonly ConcurrentDictionary<long, ITask> _runningTasks = new();

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
                _rootFolder = folder;
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
                List<IEvent> InnerFolderEvents = GetEventList(innerFolder);
                events.AddRange(InnerFolderEvents);
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
                List<ITask> innerFolderEvents = GetTaskList(innerFolder);
                tasks.AddRange(innerFolderEvents);
            }
        }

        return tasks;
    }

    private IFolder? FindFolderRecursive(IFolder folder, int folderId)
    {
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

    private Task ExecuteTask(ITask task, DynamicDataChain dataChain, DynamicDataSet lastDynamicDataSet, IPluginInstanceLogger instanceLogger)
    {
        // Get running task id
        long thisTaskId;
        lock (_lockRunningTasksCount) {
            _runningTasksCount++;
            thisTaskId = _runningTasksCount;
        }

        Task t = new(() =>
        {
            ITask? taskCopy = null;
            try
            {
                taskCopy = (ITask?)CoreHelpers.CloneObjects(task);
                if (taskCopy == null)
                    throw new ApplicationException("Cloning configuration returned null");
                
                if (taskCopy.Config.Log)
                    instanceLogger.TaskStarting(taskCopy);

                instanceLogger.Info($"About to run task {taskCopy.Config.Id} with unique running id: {thisTaskId}");
                _runningTasks.TryAdd(thisTaskId, taskCopy);
                InstanceExecResult instExecResult = taskCopy.Run(dataChain, lastDynamicDataSet, instanceLogger);
                
                if (taskCopy.Config.Log)
                    instanceLogger.TaskEnded(taskCopy);

                if (taskCopy.Connections != null)
                {
                    foreach (PluginInstanceConnection connection in taskCopy.Connections)
                    {
                        if (!connection.Enabled)
                            continue;

                        if (connection.WaitSeconds != null
                            && connection.WaitSeconds != 0)
                            Thread.Sleep((int)connection.WaitSeconds * 1000);
                        
                        ITask nextTask = (ITask)connection.ConnectTo;

                        if (!nextTask.Config.Enabled)
                            continue;

                        foreach (ExecResult execRes in instExecResult.ExecResults)
                        {
                            if (connection.EvaluateExecConditions(execRes))
                            {
                                DynamicDataChain dataChainCopy = dataChain.Clone();
                                dataChainCopy.TryAdd(taskCopy.Config.Id, execRes.Data);
                                ExecuteTask(nextTask, dataChainCopy, execRes.Data, instanceLogger);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (taskCopy != null)
                    instanceLogger.Error(taskCopy, "ExecuteTask", ex);
                else
                    instanceLogger.Error("ExecuteTask: TaskCopy object is null.", ex);
            }
        });
        t.ContinueWith(t => {
            _runningTasks.Remove(thisTaskId, out _);
        });
        t.Start();

        if (_config.SerialExecution)
            t.Wait();

        return t;
    }

    private void Plugin_EventTriggered(object sender, EventTriggeredEventArgs e)
    {
        IEvent pluginEvent = (IEvent)sender;
        e.Logger.EventTriggered(pluginEvent);
        _log.Info($"Event triggered by object: {pluginEvent.Config.Id}:{pluginEvent.Config.Name}:{pluginEvent.GetType().Name}");

        _log.Info("Building dynamic data chain");
        DynamicDataChain dataChain = [];
        dataChain.TryAdd(pluginEvent.Config.Id, e.DynamicData);

        ExecResult execResult = new(true, e.DynamicData);

        foreach (PluginInstanceConnection connection in pluginEvent.Connections)
        {
            if (!connection.Enabled)
                continue;

            if (connection.WaitSeconds != null
                && connection.WaitSeconds != 0)
                Thread.Sleep((int)connection.WaitSeconds * 1000);

            if (connection.EvaluateExecConditions(execResult))
            {
                ITask taskToRun = (ITask)connection.ConnectTo;

                if (taskToRun.Config.Enabled)
                {
                    _log.Info($"Calling ExecuteTask for: {taskToRun.Config.Id}:{taskToRun.Config.Name}:{taskToRun.GetType().Name}");
                    ExecuteTask(taskToRun, dataChain, e.DynamicData, e.Logger);
                }
                else
                {
                    _log.Info($"Task: {taskToRun.Config.Id}:{taskToRun.Config.Name}:{taskToRun.GetType().Name} disabled, skipped");
                }
            }
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
        try
        {
            _log.Info("Cleaning up old logs");
            CleanUpLog(_config.LogPath, _config.CleanUpLogsOlderThanHours);

        }
        catch (Exception ex)
        {
            _log.Error("An error occurred while cleaning up old logs", ex);
        }
    }

    public void Start()
    {
        try
        {
            Server.Core.Core.Init(_config.LogPath);

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
                    Enabled = true,
                    AutoReset = true
                };
                _logCleanTimer.Elapsed += LogCleanTimer_Elapsed;
            }

            // Initializes tasks first, then initializes events
            // This guarantee that events will not trigger untils all tasks are initialized
            _log.Info("Starting tasks initialization");
            _tasks = GetTaskList(_rootFolder);
            _tasks.ForEach(T =>
            {
                _log.Info($"Initializing task: {T.Config.Id}:{T.Config.Name}:{T.GetType().Name}");
                T.Init();
            });

            _log.Info("Starting events initialization");
            _events = GetEventList(_rootFolder);
            _events.ForEach(E =>
            {
                _log.Info($"Initializing event: {E.Config.Id}:{E.Config.Name}:{E.GetType().Name}");
                E.EventTriggered += Plugin_EventTriggered;
                E.Init();
            });
        }
        catch (Exception ex)
        {
            _log.Error("An error occurred while starting JobEngine.", ex);
        }
    }

    public void Stop()
    {
        try
        {
            _log.Info("Destroying events");
            _events.ForEach(E =>
            {
                _log.Info($"Destroying event: {E.Config.Id}:{E.Config.Name}:{E.GetType().Name}");
                E.Destroy();
            });

            _log.Info("Destroying tasks");
            _tasks.ForEach(T =>
            {
                _log.Info($"Destroying task: {T.Config.Id}:{T.Config.Name}:{T.GetType().Name}");
                T.Destroy();
            });

            // Reset running tasks tracking
            lock (_lockRunningTasksCount)
            {
                _runningTasksCount = 0;
            }
            _runningTasks.Clear();
        }
        catch (Exception ex)
        {
            _log.Error("An error occurred while stopping JobEngine.", ex);
        }
    }

    public bool StartTask(int taskID)
    {
        _log.Info($"Requested execution of task: {taskID}");

        try
        {
            DateTime now = DateTime.Now;
            ITask? taskObj = _tasks.Where(task => task.Config.Id == taskID).FirstOrDefault();

            if (taskObj == null)
            {
                _log.Info($"The task {taskID} cannot be found.");
                return false;
            }

            IPluginInstanceLogger logger = PluginInstanceLogger.GetLogger(taskObj);
            DynamicDataChain dataChain = [];
            DynamicDataSet dDataSet = CommonDynamicData.BuildStandardDynamicDataSet(taskObj, true, 0, now, now, 1);

            ExecuteTask(taskObj, dataChain, dDataSet, logger);

            return true;
        }
        catch (Exception ex)
        {
            _log.Error("An error occurred while executing the requested task.", ex);
        }
        return false;
    }

    public ReloadJobsReturnValues ReloadJobs()
    {
        _log.Info("Trying to reload job data...");

        if (!_runningTasks.IsEmpty)
        {
            _log.Info($"Cannot reload jobs now, there are {_runningTasks.Count} running tasks, please retry later.");
            return ReloadJobsReturnValues.CannotReloadWhileRunningTask;
        }


        _log.Info("Stopping and restarting JobEngine to reload jobs...");
        Stop();
        Start();

        return ReloadJobsReturnValues.Ok;
    }

    public List<IPlugin> GetPlugins()
    {
        return
        [
            new CpuEventPlugin(),
            new DateTimeEventPlugin(),
            new DiskSpaceEventPlugin(),
            new ExcelFileTaskPlugin(),
            new FileSystemEventPlugin(),
            new FileSystemTaskPlugin(),
            new FtpSftpTaskPlugin(),
            new MemoryEventPlugin(),
            new OSRobotServiceStartEventPlugin(),
            new ReadTextFileTaskPlugin(),
            new RESTApiTaskPlugin(),
            new RunProgramTaskPlugin(),
            new SendEMailTaskPlugin(),
            new SqlServerBackupTaskPlugin(),
            new SqlServerBulkCopyTaskPlugin(),
            new SqlServerCommandTaskPlugin(),
            new SystemEventsEventPlugin(),
            new UnzipTaskPlugin(),
            new WriteTextFileTaskPlugin(),
            new ZipTaskPlugin()
        ];
    }

    public IPlugin? GetPlugin(string pluginId)
    {
        string pluginFullTypeName = $"OSRobot.Server.Plugins.{pluginId}.{pluginId}Plugin, OSRobot.Server.Plugins";

        Type? pluginType = Type.GetType(pluginFullTypeName);
        if (pluginType == null)
            return null;

        return (IPlugin?)Activator.CreateInstance(pluginType);
    }

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
