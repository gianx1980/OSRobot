// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Tests.Support;

namespace OSRobot.Tests.TestClasses;

/// <summary>The engine side of the Task failure event: which failures it reports, and the loop guard.</summary>
// EngineHarness uses process-wide state (Core, ProbeLog, the trigger registry).
[TestClass]
[DoNotParallelize]
public sealed class TestTaskFailureReporting
{
    private const int FailureEventId = 9;
    private const string ProbeFailure = "Probe task configured to fail.";

    private static object FailureEvent(bool onlyUnhandled = true) => new
    {
        pluginId = "TaskFailureEvent",
        id = FailureEventId,
        name = "Failures",
        enabled = true,
        scope = "AllFolders",
        reportOnlyUnhandled = onlyUnhandled,
        groupForSeconds = 0
    };

    private static int Count(string label) => ProbeLog.Entries.Count(e => e.Label == label);

    [TestMethod]
    public void A_failed_task_fires_the_event_with_its_details()
    {
        JobGraph graph = new JobGraph()
            .Task(2, "Backup", fail: true)
            .Connect(JobGraph.EventId, 2)
            .Node(FailureEvent())
            .Task(10, "Report", value: "{object[9].FailedObjectName}|{object[9].FailureMessage}|{object[9].FailureCount}")
            .Connect(FailureEventId, 10);

        using EngineHarness harness = new(graph);
        Assert.IsTrue(harness.Fire());

        Assert.IsTrue(EngineHarness.WaitFor(() => ProbeLog.Find("Report") != null, TimeSpan.FromSeconds(10)));
        Assert.AreEqual($"Backup|{ProbeFailure}|1", ProbeLog.Find("Report")!.Value);
    }

    [TestMethod]
    public void A_failure_the_job_handles_is_not_reported()
    {
        JobGraph graph = new JobGraph()
            .Task(2, "Backup", fail: true)
            .Task(3, "OnBackupFailure")
            .Connect(JobGraph.EventId, 2)
            .Connect(2, 3, executeOperators: ["ObjectDoesNotExecute"])
            .Node(FailureEvent())
            .Task(10, "Report")
            .Connect(FailureEventId, 10);

        using EngineHarness harness = new(graph);
        Assert.IsTrue(harness.Fire());

        Assert.IsTrue(EngineHarness.WaitFor(() => ProbeLog.Find("OnBackupFailure") != null, TimeSpan.FromSeconds(10)));
        Thread.Sleep(1000);
        Assert.AreEqual(0, Count("Report"));
    }

    [TestMethod]
    public void A_handled_failure_is_reported_when_the_event_wants_every_failure()
    {
        JobGraph graph = new JobGraph()
            .Task(2, "Backup", fail: true)
            .Task(3, "OnBackupFailure")
            .Connect(JobGraph.EventId, 2)
            .Connect(2, 3, executeOperators: ["ObjectDoesNotExecute"])
            .Node(FailureEvent(onlyUnhandled: false))
            .Task(10, "Report")
            .Connect(FailureEventId, 10);

        using EngineHarness harness = new(graph);
        Assert.IsTrue(harness.Fire());

        Assert.IsTrue(EngineHarness.WaitFor(() => ProbeLog.Find("Report") != null, TimeSpan.FromSeconds(10)));
    }

    [TestMethod]
    public void A_failure_in_the_events_own_job_does_not_fire_it_again()
    {
        // The report job fails (say, the mail server is down): without the guard this would loop.
        JobGraph graph = new JobGraph()
            .Task(2, "Backup", fail: true)
            .Connect(JobGraph.EventId, 2)
            .Node(FailureEvent())
            .Task(10, "SendMail", fail: true)
            .Task(11, "Report")
            .Connect(FailureEventId, 10)
            .Connect(FailureEventId, 11);

        using EngineHarness harness = new(graph);
        Assert.IsTrue(harness.Fire());

        Assert.IsTrue(EngineHarness.WaitFor(() => ProbeLog.Find("Report") != null, TimeSpan.FromSeconds(10)));
        Thread.Sleep(1500);
        Assert.AreEqual(1, Count("Report"));
    }

    [TestMethod]
    public void The_error_message_reaches_a_does_not_execute_connection()
    {
        JobGraph graph = new JobGraph()
            .Task(2, "Backup", fail: true)
            .Task(3, "OnBackupFailure", value: "{object[2].ErrorMessage}")
            .Connect(JobGraph.EventId, 2)
            .Connect(2, 3, executeOperators: ["ObjectDoesNotExecute"]);

        using EngineHarness harness = new(graph);
        Assert.IsTrue(harness.Fire());

        Assert.IsTrue(EngineHarness.WaitFor(() => ProbeLog.Find("OnBackupFailure") != null, TimeSpan.FromSeconds(10)));
        Assert.AreEqual(ProbeFailure, ProbeLog.Find("OnBackupFailure")!.Value);
    }
}
