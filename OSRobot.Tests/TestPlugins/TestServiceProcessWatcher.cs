// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Plugins.ServiceProcessWatchEvent;

namespace OSRobot.Tests.TestPlugins;

/// <summary>
/// The change detection of the service / process watch event, on hand-made snapshots: what fires, and what doesn't.
/// </summary>
[TestClass]
public sealed class TestServiceProcessWatcher
{
    private const ServiceStatus Running = ServiceStatus.Running;
    private const ServiceStatus Stopped = ServiceStatus.Stopped;

    [TestMethod]
    [DataRow("sql*", "SQLWriter", true, DisplayName = "* matches the rest, case-insensitively")]
    [DataRow("sql*", "MSSQLSERVER", false, DisplayName = "The whole name must match")]
    [DataRow("note?ad", "notepad", true, DisplayName = "? matches one character")]
    [DataRow("note?ad", "noteppad", false, DisplayName = "? matches exactly one character")]
    [DataRow("MSSQL$SQLEXPRESS", "mssql$sqlexpress", true, DisplayName = "$ is literal")]
    [DataRow("my.app", "myXapp", false, DisplayName = ". is literal")]
    [DataRow("w3svc; sql* ", "SQLAgent", true, DisplayName = "Several patterns separated by ;")]
    [DataRow(" ; ", "anything", false, DisplayName = "No pattern matches nothing")]
    public void Names_match_wildcard_patterns(string pattern, string name, bool expected)
    {
        Assert.AreEqual(expected, new NamePattern(pattern).IsMatch(name));
    }

    // ---------
    // Services
    // ---------

    private static ServiceProcessWatchEventConfig ServiceConfig(string pattern, string conditions = "Stopped") => new()
    {
        WatchType = WatchType.Service,
        NamePattern = pattern,
        OnServiceStopped = conditions.Contains("Stopped"),
        OnServiceStarted = conditions.Contains("Started"),
        OnServicePaused = conditions.Contains("Paused"),
        OnAnyServiceStateChange = conditions.Contains("Any")
    };

    [TestMethod]
    public void The_first_service_check_is_a_baseline_that_fires_nothing()
    {
        List<ServiceInfo> services = [new("Spooler", "Print Spooler", Stopped)];
        ServiceWatcher watcher = new(ServiceConfig("*"), () => services);

        Assert.IsEmpty(watcher.Check());
    }

    [TestMethod]
    public void A_service_that_stops_fires_once_with_its_previous_and_new_state()
    {
        List<ServiceInfo> services = [new("Spooler", "Print Spooler", Running)];
        ServiceWatcher watcher = new(ServiceConfig("spooler"), () => services);
        watcher.Check();

        services = [new("Spooler", "Print Spooler", Stopped)];
        ServiceStateChange change = (ServiceStateChange)Assert.ContainsSingle(watcher.Check());

        Assert.AreEqual("Spooler", change.ServiceName);
        Assert.AreEqual("Print Spooler", change.DisplayName);
        Assert.AreEqual(Running, change.PreviousState);
        Assert.AreEqual(Stopped, change.State);
        Assert.IsEmpty(watcher.Check(), "Still stopped: nothing changed");
    }

    [TestMethod]
    public void Services_match_on_their_name_or_their_display_name()
    {
        List<ServiceInfo> services = [new("Spooler", "Print Spooler", Running), new("W3SVC", "World Wide Web Publishing Service", Running), new("Other", "Other service", Running)];
        ServiceWatcher watcher = new(ServiceConfig("print*;w3svc"), () => services);
        watcher.Check();

        services = [.. services.Select(s => s with { Status = Stopped })];
        IReadOnlyList<WatchChange> changes = watcher.Check();

        CollectionAssert.AreEquivalent(new[] { "Spooler", "W3SVC" }, changes.Cast<ServiceStateChange>().Select(c => c.ServiceName).ToArray());
    }

    [TestMethod]
    [DataRow(ServiceStatus.Running, ServiceStatus.Stopped, "Stopped", true)]
    [DataRow(ServiceStatus.Running, ServiceStatus.Stopped, "Started,Paused", false)]
    [DataRow(ServiceStatus.Stopped, ServiceStatus.Running, "Started", true)]
    [DataRow(ServiceStatus.Paused, ServiceStatus.Running, "Started", true, DisplayName = "Resumed counts as started")]
    [DataRow(ServiceStatus.Stopped, ServiceStatus.Running, "Stopped", false)]
    [DataRow(ServiceStatus.Running, ServiceStatus.Paused, "Paused", true)]
    [DataRow(ServiceStatus.Running, ServiceStatus.Paused, "Stopped,Started", false)]
    [DataRow(ServiceStatus.Running, ServiceStatus.StopPending, "Stopped", false, DisplayName = "Pending states need any change")]
    [DataRow(ServiceStatus.Running, ServiceStatus.StopPending, "Any", true)]
    [DataRow(ServiceStatus.StopPending, ServiceStatus.Stopped, "Stopped", true, DisplayName = "Stopped after a pending state seen earlier")]
    public void Service_changes_fire_only_for_the_selected_states(ServiceStatus previous, ServiceStatus state, string conditions, bool fires)
    {
        List<ServiceInfo> services = [new("Spooler", "Print Spooler", previous)];
        ServiceWatcher watcher = new(ServiceConfig("Spooler", conditions), () => services);
        watcher.Check();

        services = [new("Spooler", "Print Spooler", state)];

        Assert.HasCount(fires ? 1 : 0, watcher.Check());
    }

    [TestMethod]
    public void A_service_installed_after_the_baseline_fires_only_when_its_state_changes()
    {
        List<ServiceInfo> services = [];
        ServiceWatcher watcher = new(ServiceConfig("*", "Started"), () => services);
        watcher.Check();

        services = [new("NewService", "New service", Stopped)];
        Assert.IsEmpty(watcher.Check(), "Installing is not a state change");

        services = [new("NewService", "New service", Running)];
        Assert.ContainsSingle(watcher.Check());
    }

    // ---------
    // Processes
    // ---------

    private static ServiceProcessWatchEventConfig ProcessConfig(string pattern, bool onStarted, bool onExited) => new()
    {
        WatchType = WatchType.Process,
        NamePattern = pattern,
        OnProcessStarted = onStarted,
        OnProcessExited = onExited
    };

    private sealed class FakeProcessSource : IProcessSource
    {
        public List<ProcessInfo> Running { get; set; } = [];
        public Dictionary<int, int> ExitCodes { get; } = [];
        public HashSet<ProcessInfo> Tracked { get; } = [];
        public bool Fail { get; set; }
        public bool Disposed { get; private set; }

        public IReadOnlyList<ProcessInfo> GetProcesses() => Fail ? throw new InvalidOperationException("Cannot list the processes") : Running;

        public void Track(ProcessInfo process) => Tracked.Add(process);

        // Only a tracked process can tell its exit code, as with the real handles.
        public int? TakeExitCode(ProcessInfo process) => Tracked.Remove(process) && ExitCodes.TryGetValue(process.ProcessId, out int code) ? code : null;

        public void Dispose() => Disposed = true;
    }

    [TestMethod]
    public void Processes_running_at_the_first_check_do_not_fire()
    {
        FakeProcessSource source = new() { Running = [new(42, "notepad")] };
        ProcessWatcher watcher = new(ProcessConfig("*", onStarted: true, onExited: true), source);

        Assert.IsEmpty(watcher.Check());
    }

    [TestMethod]
    public void A_new_process_fires_started()
    {
        FakeProcessSource source = new();
        ProcessWatcher watcher = new(ProcessConfig("notepad", onStarted: true, onExited: false), source);
        watcher.Check();

        source.Running = [new(42, "notepad"), new(43, "calc")];
        ProcessStateChange change = (ProcessStateChange)Assert.ContainsSingle(watcher.Check());

        Assert.AreEqual(new ProcessStateChange(42, "notepad", ProcessChangeKind.Started, null), change);
        Assert.IsEmpty(watcher.Check(), "Still running: nothing changed");
    }

    [TestMethod]
    public void A_process_running_at_the_baseline_fires_exited_with_its_exit_code()
    {
        FakeProcessSource source = new() { Running = [new(42, "notepad")] };
        ProcessWatcher watcher = new(ProcessConfig("notepad", onStarted: false, onExited: true), source);
        watcher.Check();

        source.ExitCodes[42] = 3;
        source.Running = [];

        Assert.AreEqual(new ProcessStateChange(42, "notepad", ProcessChangeKind.Exited, 3), Assert.ContainsSingle(watcher.Check()));
    }

    [TestMethod]
    public void A_process_started_after_the_baseline_also_reports_its_exit_code()
    {
        FakeProcessSource source = new();
        ProcessWatcher watcher = new(ProcessConfig("notepad", onStarted: false, onExited: true), source);
        watcher.Check();

        source.Running = [new(42, "notepad")];
        Assert.IsEmpty(watcher.Check(), "Starts are not watched");

        source.ExitCodes[42] = 5;
        source.Running = [];

        Assert.AreEqual(5, ((ProcessStateChange)Assert.ContainsSingle(watcher.Check())).ExitCode);
    }

    [TestMethod]
    public void Nothing_is_kept_for_exit_codes_when_exits_are_not_watched()
    {
        FakeProcessSource source = new() { Running = [new(1, "notepad")] };
        ProcessWatcher watcher = new(ProcessConfig("notepad", onStarted: true, onExited: false), source);
        watcher.Check();

        source.Running = [new(1, "notepad"), new(2, "notepad")];
        watcher.Check();

        Assert.IsEmpty(source.Tracked);
    }

    [TestMethod]
    public void A_pid_reused_by_another_program_counts_as_an_exit_and_a_start()
    {
        FakeProcessSource source = new() { Running = [new(42, "notepad")] };
        ProcessWatcher watcher = new(ProcessConfig("*", onStarted: true, onExited: true), source);
        watcher.Check();

        source.Running = [new(42, "calc")];

        CollectionAssert.AreEqual(new[]
        {
            new ProcessStateChange(42, "notepad", ProcessChangeKind.Exited, null),
            new ProcessStateChange(42, "calc", ProcessChangeKind.Started, null)
        }, watcher.Check().ToArray());
    }

    [TestMethod]
    public void Processes_match_with_or_without_the_exe_extension()
    {
        FakeProcessSource source = new();
        ProcessWatcher watcher = new(ProcessConfig("notepad.exe", onStarted: true, onExited: false), source);
        watcher.Check();

        source.Running = [new(42, "notepad"), new(43, "notepad2")];

        Assert.AreEqual(42, ((ProcessStateChange)Assert.ContainsSingle(watcher.Check())).ProcessId);
    }

    [TestMethod]
    public void A_failed_first_check_leaves_the_baseline_to_the_next_one()
    {
        // Otherwise every process running after the failure would fire as started.
        FakeProcessSource source = new() { Fail = true };
        ProcessWatcher watcher = new(ProcessConfig("*", onStarted: true, onExited: true), source);
        Assert.Throws<InvalidOperationException>(() => watcher.Check());

        source.Fail = false;
        source.Running = [new(1, "notepad"), new(2, "calc")];

        Assert.IsEmpty(watcher.Check());
    }

    [TestMethod]
    public void Disposing_the_watcher_releases_the_process_source()
    {
        FakeProcessSource source = new();
        ProcessWatcher watcher = new(ProcessConfig("*", onStarted: false, onExited: true), source);

        watcher.Dispose();

        Assert.IsTrue(source.Disposed);
    }

    // ---------
    // Dynamic data
    // ---------

    [TestMethod]
    public void A_service_change_publishes_every_field_with_the_process_ones_empty()
    {
        ServiceProcessWatchEvent eventObj = new() { Config = new ServiceProcessWatchEventConfig { Id = 7, Name = "Watch 1" } };

        DynamicDataSet data = ServiceProcessWatchEvent.BuildDynamicData(eventObj, new ServiceStateChange("Spooler", "Print Spooler", Running, Stopped));

        Assert.AreEqual("Watch 1", data[CommonDynamicData.ObjectName]);
        Assert.AreEqual("Spooler", data[ServiceProcessWatchEvent.ServiceName]);
        Assert.AreEqual("Print Spooler", data[ServiceProcessWatchEvent.ServiceDisplayName]);
        Assert.AreEqual("Stopped", data[ServiceProcessWatchEvent.ServiceState]);
        Assert.AreEqual("Running", data[ServiceProcessWatchEvent.PreviousServiceState]);
        Assert.AreEqual(string.Empty, data[ServiceProcessWatchEvent.ProcessName]);
        Assert.AreEqual(string.Empty, data[ServiceProcessWatchEvent.ProcessId]);
        Assert.AreEqual(string.Empty, data[ServiceProcessWatchEvent.ProcessChange]);
        Assert.AreEqual(string.Empty, data[ServiceProcessWatchEvent.ExitCode]);
    }

    [TestMethod]
    public void A_process_exit_with_an_unknown_exit_code_publishes_it_empty()
    {
        ServiceProcessWatchEvent eventObj = new() { Config = new ServiceProcessWatchEventConfig { Id = 7, Name = "Watch 1", WatchType = WatchType.Process } };

        DynamicDataSet data = ServiceProcessWatchEvent.BuildDynamicData(eventObj, new ProcessStateChange(42, "notepad", ProcessChangeKind.Exited, null));

        Assert.AreEqual("notepad", data[ServiceProcessWatchEvent.ProcessName]);
        Assert.AreEqual(42, data[ServiceProcessWatchEvent.ProcessId]);
        Assert.AreEqual("Exited", data[ServiceProcessWatchEvent.ProcessChange]);
        Assert.AreEqual(string.Empty, data[ServiceProcessWatchEvent.ExitCode]);
        Assert.AreEqual(string.Empty, data[ServiceProcessWatchEvent.ServiceName]);
        Assert.AreEqual(string.Empty, data[ServiceProcessWatchEvent.ServiceState]);
    }
}
