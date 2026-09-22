using OSRobot.Server.JobEngineLib.Infrastructure.Abstract;
using OSRobot.Tests.Support;

namespace OSRobot.Tests.TestClasses;

/// <summary>
/// End-to-end tests of JobEngine on a real jobs.json, using the controllable TestProbeTask / TestTriggerEvent
/// plugins (discovered by PluginRegistry exactly like any third-party plugin). Not parallelizable: the engine
/// initializes process-wide state and the probes record into a shared log.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class TestJobEngine
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private static bool Ran(string label) => ProbeLog.Find(label) != null;

    [TestMethod]
    public void An_event_runs_the_connected_task_chain_and_passes_dynamic_data_along()
    {
        JobGraph graph = new JobGraph()
            .Task(2, "A", value: "hello")
            .Task(3, "B", value: "{object[2].Value}-B")
            .Connect(JobGraph.EventId, 2)
            .Connect(2, 3);
        using EngineHarness h = new(graph);

        Assert.IsTrue(h.Fire(), "The event should be live after Start().");
        Assert.IsTrue(EngineHarness.WaitFor(() => Ran("B"), Timeout), "The chain did not complete.");

        Assert.AreEqual("hello", ProbeLog.Find("A")!.Value);
        Assert.AreEqual("hello-B", ProbeLog.Find("B")!.Value, "B should see A's output through the dynamic data chain.");
        Assert.IsEmpty(h.Log.Errors, string.Join("\n", h.Log.Errors));
    }

    [TestMethod]
    public void Connection_conditions_decide_which_branch_runs()
    {
        // A fails: "B" needs A to succeed, "C" needs A to fail.
        JobGraph graph = new JobGraph()
            .Task(2, "A", fail: true)
            .Task(3, "B")
            .Task(4, "C")
            .Connect(JobGraph.EventId, 2)
            .Connect(2, 3, executeOperators: ["ObjectExecutes"])
            .Connect(2, 4, executeOperators: ["ObjectDoesNotExecute"]);
        using EngineHarness h = new(graph);

        h.Fire();
        Assert.IsTrue(EngineHarness.WaitFor(() => Ran("C"), Timeout), "The on-failure branch should run.");

        Thread.Sleep(300);
        Assert.IsFalse(Ran("B"), "The on-success branch must not run after a failure.");
    }

    [TestMethod]
    public void A_dont_execute_condition_overrides_an_execute_condition()
    {
        JobGraph graph = new JobGraph()
            .Task(2, "A")
            .Task(3, "B")
            .Task(4, "C")
            .Connect(JobGraph.EventId, 2)
            .Connect(2, 3, executeOperators: ["ObjectExecutes"], dontExecuteOperators: ["ObjectExecutes"])
            .Connect(2, 4);
        using EngineHarness h = new(graph);

        h.Fire();
        Assert.IsTrue(EngineHarness.WaitFor(() => Ran("C"), Timeout));

        Thread.Sleep(300);
        Assert.IsFalse(Ran("B"), "'Don't execute' conditions take precedence.");
    }

    [TestMethod]
    public void Disabled_connections_and_disabled_tasks_are_skipped()
    {
        JobGraph graph = new JobGraph()
            .Task(2, "A")
            .Task(3, "ViaDisabledConnection")
            .Task(4, "DisabledTask", enabled: false)
            .Task(5, "Enabled")
            .Connect(JobGraph.EventId, 2)
            .Connect(2, 3, enabled: false)
            .Connect(2, 4)
            .Connect(2, 5);
        using EngineHarness h = new(graph);

        h.Fire();
        Assert.IsTrue(EngineHarness.WaitFor(() => Ran("Enabled"), Timeout));

        Thread.Sleep(300);
        Assert.IsFalse(Ran("ViaDisabledConnection"));
        Assert.IsFalse(Ran("DisabledTask"));
    }

    [TestMethod]
    public void WaitSeconds_delays_the_next_task()
    {
        JobGraph graph = new JobGraph()
            .Task(2, "A")
            .Task(3, "B")
            .Connect(JobGraph.EventId, 2)
            .Connect(2, 3, waitSeconds: 1);
        using EngineHarness h = new(graph);

        h.Fire();
        Assert.IsTrue(EngineHarness.WaitFor(() => Ran("B"), Timeout));

        TimeSpan gap = ProbeLog.Find("B")!.Start - ProbeLog.Find("A")!.End;
        Assert.IsGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(900), gap, $"B started only {gap.TotalMilliseconds:F0} ms after A.");
    }

    [TestMethod]
    public void Sibling_connections_wait_concurrently_not_one_after_the_other()
    {
        // Two branches each wait 2 s. Concurrent: ~2 s in total. Serialized by a shared delay: ~4 s.
        JobGraph graph = new JobGraph()
            .Task(2, "A")
            .Task(3, "B")
            .Task(4, "C")
            .Connect(JobGraph.EventId, 2)
            .Connect(2, 3, waitSeconds: 2)
            .Connect(2, 4, waitSeconds: 2);
        using EngineHarness h = new(graph);

        DateTime fired = DateTime.UtcNow;
        h.Fire();
        Assert.IsTrue(EngineHarness.WaitFor(() => Ran("B") && Ran("C"), Timeout));

        TimeSpan total = DateTime.UtcNow - fired;
        Assert.IsLessThan(TimeSpan.FromSeconds(3.5), total, $"Both branches took {total.TotalSeconds:F1} s: their waits look serialized.");
    }

    [TestMethod]
    public void SerialExecution_runs_sibling_branches_one_at_a_time()
    {
        JobGraph graph = new JobGraph()
            .Task(2, "A")
            .Task(3, "B", delayMs: 400)
            .Task(4, "C", delayMs: 400)
            .Connect(JobGraph.EventId, 2)
            .Connect(2, 3)
            .Connect(2, 4);
        using EngineHarness h = new(graph, serialExecution: true);

        h.Fire();
        Assert.IsTrue(EngineHarness.WaitFor(() => Ran("B") && Ran("C"), Timeout));

        ProbeEntry first = ProbeLog.Entries.Where(e => e.Label is "B" or "C").OrderBy(e => e.Start).First();
        ProbeEntry second = ProbeLog.Entries.Where(e => e.Label is "B" or "C").OrderBy(e => e.Start).Last();
        Assert.IsGreaterThanOrEqualTo(first.End, second.Start, "In serial mode the second branch must start after the first ended.");
    }

    [TestMethod]
    public void Without_SerialExecution_sibling_branches_overlap()
    {
        JobGraph graph = new JobGraph()
            .Task(2, "A")
            .Task(3, "B", delayMs: 800)
            .Task(4, "C", delayMs: 800)
            .Connect(JobGraph.EventId, 2)
            .Connect(2, 3)
            .Connect(2, 4);
        using EngineHarness h = new(graph, serialExecution: false);

        h.Fire();
        Assert.IsTrue(EngineHarness.WaitFor(() => Ran("B") && Ran("C"), Timeout));

        ProbeEntry b = ProbeLog.Find("B")!;
        ProbeEntry c = ProbeLog.Find("C")!;
        Assert.IsLessThan(b.End, c.Start, "The branches should have run concurrently.");
    }

    [TestMethod]
    public void Stop_cancels_a_pending_connection_wait_instead_of_sleeping_it_out()
    {
        JobGraph graph = new JobGraph()
            .Task(2, "A")
            .Task(3, "B")
            .Connect(JobGraph.EventId, 2)
            .Connect(2, 3, waitSeconds: 60);
        using EngineHarness h = new(graph, stopDrainTimeoutSeconds: 20);

        h.Fire();
        Assert.IsTrue(EngineHarness.WaitFor(() => Ran("A"), Timeout));

        TimeSpan stopTime = h.Stop();

        Assert.IsLessThan(TimeSpan.FromSeconds(5), stopTime, $"Stop() took {stopTime.TotalSeconds:F1} s: the 60 s wait was not cancelled.");
        Assert.IsFalse(Ran("B"), "The delayed task must not run after Stop().");
    }

    [TestMethod]
    public void Stop_cancels_a_running_task()
    {
        JobGraph graph = new JobGraph()
            .Task(2, "Slow", delayMs: 60_000)
            .Connect(JobGraph.EventId, 2);
        using EngineHarness h = new(graph, stopDrainTimeoutSeconds: 20);

        h.Fire();
        Thread.Sleep(300);   // let it get going

        TimeSpan stopTime = h.Stop();

        Assert.IsLessThan(TimeSpan.FromSeconds(5), stopTime, $"Stop() took {stopTime.TotalSeconds:F1} s: the running task was not cancelled.");
        Assert.IsTrue(EngineHarness.WaitFor(() => ProbeLog.Find("Slow")?.Cancelled == true, TimeSpan.FromSeconds(5)),
                      "The running task should observe the cancellation.");
    }

    [TestMethod]
    public void Stop_does_not_hang_when_events_are_firing_at_the_same_time()
    {
        // Regression: a dispatch that was accepted just before Stop() but whose work item was cancelled
        // before starting used to leak its "active dispatch" count, making Stop() wait out the full drain timeout.
        for (int round = 0; round < 15; round++)
        {
            JobGraph graph = new JobGraph()
                .Task(2, "A", delayMs: 5)
                .Connect(JobGraph.EventId, 2);
            using EngineHarness h = new(graph, stopDrainTimeoutSeconds: 8);

            Task[] firers = [.. Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < 50; i++)
                    h.Fire();
            }))];

            Thread.Sleep(Random.Shared.Next(0, 15));
            TimeSpan stopTime = h.Stop();
            Task.WaitAll(firers, TimeSpan.FromSeconds(10));

            Assert.IsLessThan(TimeSpan.FromSeconds(4), stopTime, $"Round {round}: Stop() took {stopTime.TotalSeconds:F1} s.");
            Assert.IsEmpty(h.Log.Warnings.Where(w => w.Contains("still in flight")), $"Round {round}: dispatches were left in flight.");
        }
    }

    [TestMethod]
    public async Task StartTaskAsync_runs_a_task_by_id_and_reports_unknown_ids()
    {
        JobGraph graph = new JobGraph()
            .Task(2, "Manual", value: "manual-run")
            .Connect(JobGraph.EventId, 2);
        using EngineHarness h = new(graph);

        // In the default (non-serial) mode StartTaskAsync returns once the task is dispatched, not finished.
        Assert.IsTrue(await h.Engine.StartTaskAsync(2));
        Assert.IsTrue(EngineHarness.WaitFor(() => Ran("Manual"), Timeout), "The task did not run.");
        Assert.AreEqual("manual-run", ProbeLog.Find("Manual")!.Value);

        Assert.IsFalse(await h.Engine.StartTaskAsync(999), "An unknown task id should not be reported as started.");
    }

    [TestMethod]
    public async Task ReloadJobs_is_refused_while_a_task_is_running()
    {
        JobGraph graph = new JobGraph()
            .Task(2, "Long", delayMs: 1500)
            .Connect(JobGraph.EventId, 2);
        using EngineHarness h = new(graph);

        Assert.IsTrue(await h.Engine.StartTaskAsync(2));
        Assert.IsTrue(EngineHarness.WaitFor(() => h.Engine.ReloadJobs() == ReloadJobsReturnValues.CannotReloadWhileRunningTask,
                                            TimeSpan.FromSeconds(1)),
                      "Reload should be refused while the task is running.");

        Assert.IsTrue(EngineHarness.WaitFor(() => Ran("Long"), Timeout));
        Assert.IsTrue(EngineHarness.WaitFor(() => h.Engine.ReloadJobs() == ReloadJobsReturnValues.Ok, Timeout),
                      "Reload should be allowed again once the task has finished.");
    }

    [TestMethod]
    public async Task Cancelling_the_caller_token_cancels_the_task_in_serial_mode()
    {
        // Serial mode: StartTaskAsync only returns when the task is done, so the caller is still waiting
        // and its cancellation (e.g. an aborted HTTP request) must reach the task.
        JobGraph graph = new JobGraph()
            .Task(2, "Long", delayMs: 30_000)
            .Connect(JobGraph.EventId, 2);
        using EngineHarness h = new(graph, serialExecution: true, stopDrainTimeoutSeconds: 20);
        using CancellationTokenSource cts = new();

        Task<bool> start = h.Engine.StartTaskAsync(2, cts.Token);
        Thread.Sleep(300);
        cts.Cancel();

        Assert.IsTrue(EngineHarness.WaitFor(() => ProbeLog.Find("Long")?.Cancelled == true, TimeSpan.FromSeconds(3)),
                      "The running task should observe the caller's cancellation.");
        await start;
    }

    [TestMethod]
    public async Task Cancelling_the_caller_token_does_not_cancel_a_task_already_dispatched_in_the_background()
    {
        // Non-serial mode: StartTaskAsync returns as soon as the task is dispatched, like an HTTP request
        // that has been answered "started". The request's token must not kill the task afterwards.
        JobGraph graph = new JobGraph()
            .Task(2, "Bg", delayMs: 800)
            .Connect(JobGraph.EventId, 2);
        using EngineHarness h = new(graph);
        using CancellationTokenSource cts = new();

        Assert.IsTrue(await h.Engine.StartTaskAsync(2, cts.Token));
        cts.Cancel();

        Assert.IsTrue(EngineHarness.WaitFor(() => Ran("Bg"), Timeout), "The background task should still complete.");
        Assert.IsFalse(ProbeLog.Find("Bg")!.Cancelled);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Stop_cancels_a_manually_started_task(bool serialExecution)
    {
        JobGraph graph = new JobGraph()
            .Task(2, "Long", delayMs: 30_000)
            .Connect(JobGraph.EventId, 2);
        using EngineHarness h = new(graph, serialExecution: serialExecution, stopDrainTimeoutSeconds: 20);

        Task<bool> start = h.Engine.StartTaskAsync(2);
        Thread.Sleep(300);

        TimeSpan stopTime = h.Stop();

        Assert.IsLessThan(TimeSpan.FromSeconds(5), stopTime, $"Stop() took {stopTime.TotalSeconds:F1} s: the task was not cancelled.");
        Assert.IsTrue(EngineHarness.WaitFor(() => ProbeLog.Find("Long")?.Cancelled == true, TimeSpan.FromSeconds(3)));
        await start;
    }

    [TestMethod]
    public void Events_stop_being_delivered_after_Stop()
    {
        JobGraph graph = new JobGraph()
            .Task(2, "A")
            .Connect(JobGraph.EventId, 2);
        using EngineHarness h = new(graph);

        h.Stop();

        Assert.IsFalse(h.Fire(), "The event instance is destroyed by Stop().");
        Thread.Sleep(200);
        Assert.IsFalse(Ran("A"));
    }
}
