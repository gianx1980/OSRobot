// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging;
using OSRobot.Server.Core.Logging.Abstract;

namespace OSRobot.Server.Plugins.ServiceProcessWatchEvent;

/// <summary>
/// Fires when a Windows service changes state, or a process starts or exits. It polls: every check compares the
/// services or processes with those of the previous check (see <see cref="IWatcher"/>). WMI events were left out:
/// they need the System.Management package and administrator rights for the process traces, exist on Windows
/// only, and poll anyway for service changes, while listing all the services or processes costs a few
/// milliseconds and works the same on Linux.
/// </summary>
public class ServiceProcessWatchEvent : IEvent
{
    public const string ServiceName = "ServiceName";
    public const string ServiceDisplayName = "ServiceDisplayName";
    public const string ServiceState = "ServiceState";
    public const string PreviousServiceState = "PreviousServiceState";
    public const string ProcessName = "ProcessName";
    public const string ProcessId = "ProcessId";
    public const string ProcessChange = "ProcessChange";
    public const string ExitCode = "ExitCode";

    public IFolder? ParentFolder { get; set; }

    public IPluginInstanceConfig Config { get; set; } = new ServiceProcessWatchEventConfig();

    public List<PluginInstanceConnection> Connections { get; set; } = [];

    private IEventSink? _sink;

    private readonly System.Timers.Timer _recurringTimer = new();

    // Each check compares with the previous one, so checks never overlap: one still running when the timer
    // elapses again is not doubled. Destroy takes it too, so the watcher isn't disposed in the middle of a check.
    private readonly object _checkGate = new();

    private IWatcher? _watcher;

    private bool _destroyed;

    // The same error at every check is logged once: a log file each few seconds would fill the disk.
    private string? _lastError;

    public void Init(IEventSink sink)
    {
        _sink = sink;

        ServiceProcessWatchEventConfig config = (ServiceProcessWatchEventConfig)Config;

        // Logged once, not at every check: neither can change until the job is changed.
        if (new NamePattern(config.NamePattern).IsEmpty)
        {
            PluginInstanceLogger.GetLogger(this).Error(this, "No name to watch: the event does not start.");
            return;
        }

        if (config.WatchType == WatchType.Process)
            _watcher = new ProcessWatcher(config, new SystemProcessSource());
        else if (OperatingSystem.IsWindows())
            _watcher = new ServiceWatcher(config, WindowsServices.Read);
        else
        {
            PluginInstanceLogger.GetLogger(this).Error(this, "Windows services can be watched on Windows only: the event does not start.");
            return;
        }

        // The baseline, taken now rather than at the first tick, so a change right after the start is not missed.
        Check();

        int checkIntervalSeconds = config.CheckIntervalSeconds;
        if (checkIntervalSeconds <= 0)
            checkIntervalSeconds = 1;

        _recurringTimer.AutoReset = true;
        _recurringTimer.Interval = TimeSpan.FromSeconds(checkIntervalSeconds).TotalMilliseconds;
        _recurringTimer.Elapsed += RecurringTimer_Elapsed;
        _recurringTimer.Enabled = true;
    }

    public void Destroy()
    {
        _recurringTimer.Dispose();

        lock (_checkGate)
        {
            _destroyed = true;
            _watcher?.Dispose();
        }
    }

    private void RecurringTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        Check();
    }

    private void Check()
    {
        if (!Monitor.TryEnter(_checkGate))
            return;

        try
        {
            if (_destroyed || _watcher == null)
                return;

            IReadOnlyList<WatchChange> changes = _watcher.Check();
            _lastError = null;

            foreach (WatchChange change in changes)
            {
                // A logger per occurrence: each one is a run of its own, with its own log.
                IPluginInstanceLogger logger = PluginInstanceLogger.GetLogger(this);
                DynamicDataSet dDataSet = BuildDynamicData(this, change);

                if (Config.Log)
                {
                    logger.Info(this, Describe(change));
                    logger.EventTriggering(this);
                }

                _sink?.Publish(this, dDataSet, logger);
            }
        }
        catch (Exception ex)
        {
            // Errors are always logged, regardless of Config.Log: an event has no ExecResult to
            // report failure through, so without this the event would fail silently forever.
            if (ex.Message != _lastError)
            {
                _lastError = ex.Message;
                PluginInstanceLogger.GetLogger(this).EventError(this, ex);
            }
        }
        finally
        {
            Monitor.Exit(_checkGate);
        }
    }

    /// <summary>
    /// Every field is always published, empty when it belongs to the other kind of watch: a connection condition
    /// on it then reads an empty value instead of failing.
    /// </summary>
    public static DynamicDataSet BuildDynamicData(IPluginInstance source, WatchChange change)
    {
        ServiceStateChange? service = change as ServiceStateChange;
        ProcessStateChange? process = change as ProcessStateChange;

        DateTime now = DateTime.Now;
        DynamicDataSet dDataSet = CommonDynamicData.BuildStandardDynamicDataSet(source, true, 0, now, now, 1);
        dDataSet.TryAdd(ServiceName, service?.ServiceName ?? string.Empty);
        dDataSet.TryAdd(ServiceDisplayName, service?.DisplayName ?? string.Empty);
        dDataSet.TryAdd(ServiceState, service?.State.ToString() ?? string.Empty);
        dDataSet.TryAdd(PreviousServiceState, service?.PreviousState.ToString() ?? string.Empty);
        dDataSet.TryAdd(ProcessName, process?.ProcessName ?? string.Empty);
        dDataSet.TryAdd(ProcessId, (object?)process?.ProcessId ?? string.Empty);
        dDataSet.TryAdd(ProcessChange, process?.Kind.ToString() ?? string.Empty);
        dDataSet.TryAdd(ExitCode, (object?)process?.ExitCode ?? string.Empty);

        return dDataSet;
    }

    private static string Describe(WatchChange change) => change switch
    {
        ServiceStateChange service => $"Service {service.ServiceName} ({service.DisplayName}): {service.PreviousState} -> {service.State}",
        ProcessStateChange { Kind: ProcessChangeKind.Exited } process => $"Process {process.ProcessName} ({process.ProcessId}) exited, exit code: {process.ExitCode?.ToString() ?? "unknown"}",
        ProcessStateChange process => $"Process {process.ProcessName} ({process.ProcessId}) started",
        _ => change.ToString()
    };
}
