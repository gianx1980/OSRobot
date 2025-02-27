﻿/*======================================================================================
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

namespace OSRobot.Server.JobEngineLib;

public class JobEngine : IJobEngine
{
    private IAppLogger _log;
    private IJobEngineConfig _config;
    private IFolder _rootFolder;

    private List<IEvent> _events;
    private List<ITask> _tasks;
    private System.Timers.Timer _logCleanTimer;

    // Keep track of running tasks
    private object _lockRunningTasksCount = new object();
    private long _runningTasksCount;
    private ConcurrentDictionary<long, ITask> _runningTasks;

    public JobEngine(IAppLogger appLogger, IJobEngineConfig config)
    {
        _log = appLogger;
        _config = config;
        _rootFolder = new Folder();
        _events = new List<IEvent>();
        _tasks = new List<ITask>();
        _logCleanTimer = new System.Timers.Timer();
        _runningTasks = new ConcurrentDictionary<long, ITask>();
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
        List<IEvent> events = new List<IEvent>();

        foreach (IPluginInstanceBase pluginInstance in folder)
        {
            if (pluginInstance is IEvent)
            {
                // Skip disabled events
                if (!pluginInstance.Config.Enabled)
                    continue;

                events.Add((IEvent)pluginInstance);
            }
            else if (pluginInstance is IFolder)
            {
                List<IEvent> InnerFolderEvents = GetEventList((IFolder)pluginInstance);
                events.AddRange(InnerFolderEvents);
            }
        }

        return events;
    }

    private List<ITask> GetTaskList(IFolder folder)
    {
        List<ITask> tasks = new List<ITask>();

        foreach (IPluginInstanceBase pluginInstance in folder)
        {
            if (pluginInstance is ITask)
            {
                tasks.Add((ITask)pluginInstance);
            }
            else if (pluginInstance is IFolder)
            {
                List<ITask> innerFolderEvents = GetTaskList((IFolder)pluginInstance);
                tasks.AddRange(innerFolderEvents);
            }
        }

        return tasks;
    }

    private Task ExecuteTask(ITask task, DynamicDataChain dataChain, DynamicDataSet lastDynamicDataSet, IPluginInstanceLogger instanceLogger)
    {
        // Get running task id
        long thisTaskId;
        lock (_lockRunningTasksCount) {
            _runningTasksCount++;
            thisTaskId = _runningTasksCount;
        }

        Task t = new Task(() =>
        {
            ITask? taskCopy = null;
            try
            {
                taskCopy = (ITask?)CoreHelpers.CloneObjects(task);
                if (taskCopy == null)
                    throw new ApplicationException("Cloning configuration returned null");
                DynamicDataSet? lastDataSetCopy = (DynamicDataSet?)CoreHelpers.CloneObjects(lastDynamicDataSet);
                if (lastDataSetCopy == null)
                    throw new ApplicationException("Cloning configuration returned null");

                if (taskCopy.Config.Log)
                    instanceLogger.TaskStarting(taskCopy);

                instanceLogger.Info($"About to run task {taskCopy.Config.Id} with unique running id: {thisTaskId}");
                _runningTasks.TryAdd(thisTaskId, taskCopy);
                InstanceExecResult instExecResult = taskCopy.Run(dataChain, lastDataSetCopy, instanceLogger);
                
                if (taskCopy.Config.Log)
                    instanceLogger.TaskEnded(taskCopy);

                if (taskCopy.Connections != null)
                {
                    foreach (PluginInstanceConnection connection in taskCopy.Connections)
                    {
                        if (!connection.Enabled)
                            continue;

                        if (connection.WaitSeconds != null)
                            Thread.Sleep((int)connection.WaitSeconds * 1000);
                        
                        ITask nextTask = (ITask)connection.ConnectTo;

                        if (!nextTask.Config.Enabled)
                            continue;

                        foreach (ExecResult execRes in instExecResult.execResults)
                        {
                            if (connection.EvaluateExecConditions(execRes))
                            {
                                DynamicDataChain? dataChainCopy = (DynamicDataChain?)CoreHelpers.CloneObjects(dataChain);
                                if (dataChainCopy == null)
                                    throw new ApplicationException("Cloning configuration returned null");
                                dataChainCopy.Add(taskCopy.Config.Id, execRes.Data);
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
        DynamicDataChain DataChain = new DynamicDataChain();
        DataChain.Add(pluginEvent.Config.Id, e.DynamicData);

        ExecResult execResult = new ExecResult(true, e.DynamicData);

        foreach (PluginInstanceConnection connection in pluginEvent.Connections)
        {
            if (!connection.Enabled)
                continue;

            if (connection.WaitSeconds != null)
                Thread.Sleep((int)connection.WaitSeconds * 1000);

            if (connection.EvaluateExecConditions(execResult))
            {
                ITask taskToRun = (ITask)connection.ConnectTo;

                if (taskToRun.Config.Enabled)
                {
                    _log.Info($"Calling ExecuteTask for: {taskToRun.Config.Id}:{taskToRun.Config.Name}:{taskToRun.GetType().Name}");
                    ExecuteTask(taskToRun, DataChain, e.DynamicData, e.Logger);
                }
                else
                {
                    _log.Info($"Task: {taskToRun.Config.Id}:{taskToRun.Config.Name}:{taskToRun.GetType().Name} disabled, skipped");
                }
            }
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

                _logCleanTimer = new System.Timers.Timer();
                _logCleanTimer.Interval = new TimeSpan(0, _config.CleanUpLogsIntervalHours, 0, 0).TotalMilliseconds;
                _logCleanTimer.Enabled = true;
                _logCleanTimer.AutoReset = true;
                _logCleanTimer.Elapsed += _LogCleanTimer_Elapsed;
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
            FileInfo fi = new FileInfo(fullPathFileName);
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

    private void _LogCleanTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
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
            DynamicDataChain dataChain = new DynamicDataChain();
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

        if (_runningTasks.Count > 0)
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
        return new List<IPlugin>()
        {
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
        };
    }

    public IPlugin? GetPlugin(string pluginId)
    {
        string pluginFullTypeName = $"OSRobot.Server.Plugins.{pluginId}.{pluginId}Plugin, OSRobot.Server.Plugins";

        Type? pluginType = Type.GetType(pluginFullTypeName);
        if (pluginType == null)
            return null;

        return (IPlugin?)Activator.CreateInstance(pluginType);
    }
}
