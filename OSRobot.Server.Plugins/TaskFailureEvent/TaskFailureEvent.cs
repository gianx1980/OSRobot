// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Collections.Concurrent;
using System.Data;
using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging;
using OSRobot.Server.Core.Logging.Abstract;

namespace OSRobot.Server.Plugins.TaskFailureEvent;

/// <summary>
/// Fires when tasks fail, so one job can react to the failures of many (send an e-mail, a notification...)
/// without an "Object does not execute" connection on every task. The engine reports each failure from a
/// worker thread (<see cref="ITaskFailureListener"/>); the event publishes from a thread of its own: right
/// away, or at the end of a grouping window opened by the first failure.
/// </summary>
public class TaskFailureEvent : IEvent, ITaskFailureListener
{
    /// <summary>The Failures recordset keeps at most this many rows; FailureCount still counts them all.</summary>
    public const int MaxFailuresPerEvent = 500;

    public const string FailedObjectID = "FailedObjectID";
    public const string FailedObjectName = "FailedObjectName";
    public const string FailedObjectType = "FailedObjectType";
    public const string FailedObjectFolder = "FailedObjectFolder";
    public const string FailureMessage = "FailureMessage";
    public const string FailedIterationIndex = "FailedIterationIndex";
    public const string FailureDate = "FailureDate";
    public const string FailureCount = "FailureCount";
    public const string Failures = "Failures";

    // Plugin titles by task type, for FailedObjectType: looked up once per type.
    private static readonly ConcurrentDictionary<Type, string> _typeTitles = new();

    public IFolder? ParentFolder { get; set; }

    public IPluginInstanceConfig Config { get; set; } = new TaskFailureEventConfig();

    public List<PluginInstanceConnection> Connections { get; set; } = [];

    private IEventSink? _sink;

    // Guards the grouping window and _destroyed.
    private readonly object _gate = new();
    private List<TaskFailure> _pending = [];
    private int _pendingCount;
    private Timer? _windowTimer;
    private bool _destroyed;

    public void Init(IEventSink sink)
    {
        _sink = sink;
    }

    public void OnTaskFailed(TaskFailure failure)
    {
        TaskFailureEventConfig config = (TaskFailureEventConfig)Config;

        if ((failure.Handled && config.ReportOnlyUnhandled) || !IsInScope(failure.Task, config.Scope))
            return;

        lock (_gate)
        {
            if (_destroyed)
                return;

            if (config.GroupForSeconds <= 0)
            {
                // Off the engine's worker thread, like any other event's occurrence.
                _ = Task.Run(() => Publish([failure], 1));
                return;
            }

            if (_pending.Count < MaxFailuresPerEvent)
                _pending.Add(failure);
            _pendingCount++;

            // The first failure opens the window; it closes after a fixed time, whatever happens meanwhile,
            // so a job failing over and over is still reported.
            _windowTimer ??= new Timer(_ => CloseWindow(), null, TimeSpan.FromSeconds(config.GroupForSeconds), Timeout.InfiniteTimeSpan);
        }
    }

    private void CloseWindow()
    {
        List<TaskFailure> failures;
        int count;

        lock (_gate)
        {
            failures = _pending;
            count = _pendingCount;
            _pending = [];
            _pendingCount = 0;
            _windowTimer?.Dispose();
            _windowTimer = null;

            if (_destroyed || count == 0)
                return;
        }

        Publish(failures, count);
    }

    private void Publish(List<TaskFailure> failures, int count)
    {
        IPluginInstanceLogger logger = PluginInstanceLogger.GetLogger(this);

        try
        {
            DateTime now = DateTime.Now;
            DynamicDataSet dDataSet = CommonDynamicData.BuildStandardDynamicDataSet(this, true, 0, now, now, 1);

            // The single fields describe the first failure; the recordset has them all.
            TaskFailure first = failures[0];
            dDataSet.TryAdd(FailedObjectID, first.Task.Config.Id);
            dDataSet.TryAdd(FailedObjectName, first.Task.Config.Name);
            dDataSet.TryAdd(FailedObjectType, GetTypeTitle(first.Task));
            dDataSet.TryAdd(FailedObjectFolder, GetFolderPath(first.Task));
            dDataSet.TryAdd(FailureMessage, first.ErrorMessage);
            dDataSet.TryAdd(FailedIterationIndex, first.IterationIndex ?? -1);
            dDataSet.TryAdd(FailureDate, first.FailureDate);
            dDataSet.TryAdd(FailureCount, count);
            dDataSet.TryAdd(Failures, BuildFailuresRecordset(failures));

            if (Config.Log)
            {
                logger.Info(this, count == 1 ? $"Task {first.Task.Config.Id}:{first.Task.Config.Name} failed"
                                             : $"{count} task failures, starting with {first.Task.Config.Id}:{first.Task.Config.Name}");
                logger.EventTriggering(this);
            }

            _sink?.Publish(this, dDataSet, logger);
        }
        catch (Exception ex)
        {
            // Errors are always logged, regardless of Config.Log: an event has no ExecResult to report them through.
            logger.EventError(this, ex);
        }
    }

    private static DataTable BuildFailuresRecordset(List<TaskFailure> failures)
    {
        DataTable table = new(Failures);
        table.Columns.Add("ObjectID", typeof(int));
        table.Columns.Add("ObjectName", typeof(string));
        table.Columns.Add("ObjectType", typeof(string));
        table.Columns.Add("Folder", typeof(string));
        table.Columns.Add("Message", typeof(string));
        table.Columns.Add("IterationIndex", typeof(int));
        table.Columns.Add("FailureDate", typeof(DateTime));

        foreach (TaskFailure failure in failures)
        {
            table.Rows.Add(failure.Task.Config.Id, failure.Task.Config.Name, GetTypeTitle(failure.Task), GetFolderPath(failure.Task),
                           failure.ErrorMessage, failure.IterationIndex ?? -1, failure.FailureDate);
        }

        return table;
    }

    private bool IsInScope(ITask task, TaskFailureEventScope scope)
    {
        if (scope == TaskFailureEventScope.AllFolders)
            return true;

        if (scope == TaskFailureEventScope.ThisFolder)
            return IsSameFolder(task.ParentFolder, ParentFolder);

        for (IFolder? folder = task.ParentFolder; folder != null; folder = folder.ParentFolder)
        {
            if (IsSameFolder(folder, ParentFolder))
                return true;
        }

        return false;
    }

    private static bool IsSameFolder(IFolder? a, IFolder? b)
    {
        return a != null && b != null && a.Config.Id == b.Config.Id;
    }

    /// <summary>The folder names below the root, e.g. "Backups/Nightly"; empty for a task in the root folder.</summary>
    private static string GetFolderPath(ITask task)
    {
        List<string> names = [];
        for (IFolder? folder = task.ParentFolder; folder?.ParentFolder != null; folder = folder.ParentFolder)
            names.Insert(0, folder.Config.Name);

        return string.Join("/", names);
    }

    private static string GetTypeTitle(ITask task)
    {
        return _typeTitles.GetOrAdd(task.GetType(), type =>
            PluginRegistry.GetPlugins().FirstOrDefault(p => p.GetInstance().GetType() == type)?.Title ?? type.Name);
    }

    public void Destroy()
    {
        int notReported;

        lock (_gate)
        {
            _destroyed = true;
            _windowTimer?.Dispose();
            _windowTimer = null;
            notReported = _pendingCount;
            _pending = [];
            _pendingCount = 0;
        }

        // The engine is stopping or reloading: it accepts no more runs, so an open window can't fire.
        if (notReported > 0)
            PluginInstanceLogger.GetLogger(this).Error(this, $"{notReported} task failure(s) not reported: the engine stopped before the grouping window ended.");
    }
}
