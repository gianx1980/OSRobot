// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Diagnostics;
using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Plugins.ServiceProcessWatchEvent;
using OSRobot.Tests.Support;

namespace OSRobot.Tests.TestPlugins;

/// <summary>
/// The service / process watch event on the real machine. Other tests start the same programs in parallel,
/// so each test looks only for the process id it started itself.
/// </summary>
[TestClass]
public sealed class TestServiceProcessWatchEvent
{
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(20);

    private static void RequireProcessWatching()
    {
        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux())
            Assert.Inconclusive("Process watching is supported on Windows and Linux only.");
    }

    private static Process StartProgram(string fileName, params string[] arguments)
    {
        ProcessStartInfo startInfo = new(fileName, arguments)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true
        };

        return Process.Start(startInfo) ?? throw new InvalidOperationException($"{fileName} did not start.");
    }

    private static void Stop(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill();
        }
        catch (InvalidOperationException)
        {
            // Already gone.
        }

        process.Dispose();
    }

    private static ServiceProcessWatchEvent CreateEvent(ServiceProcessWatchEventConfig config) => new()
    {
        ParentFolder = Common.CreateRootFolder(),
        Config = config
    };

    private static bool IsChangeOf(DynamicDataSet data, int processId, ProcessChangeKind kind) =>
        data[ServiceProcessWatchEvent.ProcessId] is int id && id == processId
        && (string)data[ServiceProcessWatchEvent.ProcessChange] == kind.ToString();

    [TestMethod]
    public async Task A_process_started_after_init_fires_started()
    {
        RequireProcessWatching();

        ServiceProcessWatchEvent eventObj = CreateEvent(new()
        {
            Id = 1,
            Name = "Process watch 1",
            WatchType = WatchType.Process,
            NamePattern = OperatingSystem.IsWindows() ? "ping" : "sleep",
            OnProcessStarted = true,
            OnProcessExited = false,
            CheckIntervalSeconds = 1
        });
        RecordingEventSink sink = new();
        Process? process = null;

        try
        {
            eventObj.Init(sink);

            process = OperatingSystem.IsWindows() ? StartProgram("ping", "-n", "30", "127.0.0.1") : StartProgram("sleep", "30");
            int processId = process.Id;

            DynamicDataSet? data = await sink.WaitForPublishedAsync(d => IsChangeOf(d, processId, ProcessChangeKind.Started), _timeout);

            Assert.IsNotNull(data, "The start was not detected.");
            Assert.AreEqual(OperatingSystem.IsWindows() ? "ping" : "sleep", (string)data[ServiceProcessWatchEvent.ProcessName], ignoreCase: true);
            Assert.AreEqual(string.Empty, data[ServiceProcessWatchEvent.ExitCode]);
        }
        finally
        {
            eventObj.Destroy();
            if (process != null)
                Stop(process);
        }
    }

    [TestMethod]
    public async Task A_process_running_at_init_fires_exited_with_its_exit_code()
    {
        RequireProcessWatching();

        // Running when the event starts, so it is in the baseline: there is no start to wait for.
        Process process = OperatingSystem.IsWindows()
            ? StartProgram(Path.Combine(Environment.SystemDirectory, "cmd.exe"), "/c", "ping -n 4 127.0.0.1 > nul & exit /b 7")
            : StartProgram("/bin/sh", "-c", "sleep 3; exit 7");
        ServiceProcessWatchEvent eventObj = CreateEvent(new()
        {
            Id = 1,
            Name = "Process watch 1",
            WatchType = WatchType.Process,
            NamePattern = OperatingSystem.IsWindows() ? "cmd" : "sh",
            OnProcessStarted = false,
            OnProcessExited = true,
            CheckIntervalSeconds = 1
        });
        RecordingEventSink sink = new();

        try
        {
            eventObj.Init(sink);

            DynamicDataSet? data = await sink.WaitForPublishedAsync(d => IsChangeOf(d, process.Id, ProcessChangeKind.Exited), _timeout);

            Assert.IsNotNull(data, "The exit was not detected.");
            // Only the parent can read a process's exit status on Linux.
            object expectedExitCode = OperatingSystem.IsWindows() ? 7 : string.Empty;
            Assert.AreEqual(expectedExitCode, data[ServiceProcessWatchEvent.ExitCode]);
        }
        finally
        {
            eventObj.Destroy();
            Stop(process);
        }
    }

    [TestMethod]
    public void Reads_the_state_of_the_real_services()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Inconclusive("Windows services exist on Windows only.");
            return;
        }

        // The RPC service runs on every Windows machine and cannot be stopped.
        ServiceInfo rpc = WindowsServices.Read().Single(s => s.ServiceName.Equals("RpcSs", StringComparison.OrdinalIgnoreCase));

        Assert.AreEqual(ServiceStatus.Running, rpc.Status);
        Assert.IsFalse(string.IsNullOrEmpty(rpc.DisplayName));
    }

    [TestMethod]
    public void The_plugin_publishes_the_fields_it_describes()
    {
        IPlugin plugin = new ServiceProcessWatchEventPlugin();
        ServiceProcessWatchEvent eventObj = (ServiceProcessWatchEvent)plugin.GetInstance();
        eventObj.Config = plugin.GetPluginDefaultConfig();

        DynamicDataSet data = ServiceProcessWatchEvent.BuildDynamicData(eventObj, new ProcessStateChange(42, "notepad", ProcessChangeKind.Started, null));

        // A field described to the editor but never published would be a broken reference at run time.
        CollectionAssert.IsSubsetOf(plugin.SampleDynamicData.Select(s => s.InternalName).ToArray(), data.Keys.ToArray());
    }
}
