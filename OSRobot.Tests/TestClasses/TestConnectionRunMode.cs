// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Tests.Support;

namespace OSRobot.Tests.TestClasses;

/// <summary>
/// End-to-end tests of a connection's run mode: once per result (each iteration) or once with all the results.
/// Not parallelizable, like TestJobEngine: the engine initializes process-wide state and the probes record into a shared log.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class TestConnectionRunMode
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private static List<ProbeEntry> Runs(string label) => [.. ProbeLog.Entries.Where(e => e.Label == label)];

    [TestMethod]
    public void By_default_the_target_runs_once_per_iteration()
    {
        // No runMode in the JSON, as in jobs saved before the setting existed.
        JobGraph graph = new JobGraph()
            .IteratingTask(2, "A", iterations: 3)
            .Task(3, "B", value: "{object[2].Iteration}")
            .Connect(JobGraph.EventId, 2)
            .Connect(2, 3);
        using EngineHarness h = new(graph);

        h.Fire();
        Assert.IsTrue(EngineHarness.WaitFor(() => Runs("B").Count == 3, Timeout), $"B ran {Runs("B").Count} time(s) instead of 3.");

        CollectionAssert.AreEquivalent(new[] { "0", "1", "2" }, Runs("B").Select(e => e.Value).ToList());
    }

    [TestMethod]
    public void Once_with_all_results_runs_the_target_once_and_passes_every_iteration()
    {
        JobGraph graph = new JobGraph()
            .IteratingTask(2, "A", iterations: 3)
            .Task(3, "B", value: "{object[2].Results[0]['Iteration']},{object[2].Results[2]['Iteration']}|{object[2].Iteration}|{object[2].NumberOfIterations}")
            .Connect(JobGraph.EventId, 2)
            .Connect(2, 3, runMode: "OnceWithAllResults");
        using EngineHarness h = new(graph);

        h.Fire();
        Assert.IsTrue(EngineHarness.WaitFor(() => Runs("B").Count == 1, Timeout), "B did not run.");

        Thread.Sleep(300);
        Assert.HasCount(1, Runs("B"), "B must run once for the whole execution, not once per iteration.");

        // Results holds every iteration; the other fields hold the last iteration's values.
        Assert.AreEqual("0,2|2|3", Runs("B")[0].Value);
        Assert.IsEmpty(h.Log.Errors, string.Join("\n", h.Log.Errors));
    }

    [TestMethod]
    public void The_collected_result_rule_decides_whether_a_partial_failure_counts_as_success()
    {
        // Iteration 1 of 3 fails.
        JobGraph graph = new JobGraph()
            .IteratingTask(2, "A", iterations: 3, failOnIteration: 1)
            .Task(3, "AllSucceeded")
            .Task(4, "NotAllSucceeded")
            .Task(5, "AnySucceeded")
            .Connect(JobGraph.EventId, 2)
            .Connect(2, 3, runMode: "OnceWithAllResults", collectedResultRule: "AllSucceeded", executeOperators: ["ObjectExecutes"])
            .Connect(2, 4, runMode: "OnceWithAllResults", collectedResultRule: "AllSucceeded", executeOperators: ["ObjectDoesNotExecute"])
            .Connect(2, 5, runMode: "OnceWithAllResults", collectedResultRule: "AnySucceeded", executeOperators: ["ObjectExecutes"]);
        using EngineHarness h = new(graph);

        h.Fire();
        Assert.IsTrue(EngineHarness.WaitFor(() => Runs("NotAllSucceeded").Count == 1 && Runs("AnySucceeded").Count == 1, Timeout));

        Thread.Sleep(300);
        Assert.IsEmpty(Runs("AllSucceeded"), "With one failed iteration, 'all succeeded' is not satisfied.");
        Assert.HasCount(1, Runs("NotAllSucceeded"));
        Assert.HasCount(1, Runs("AnySucceeded"));
    }

    [TestMethod]
    public void With_zero_iterations_there_is_nothing_to_collect_and_the_target_does_not_run()
    {
        JobGraph graph = new JobGraph()
            .IteratingTask(2, "A", iterations: 0)
            .Task(3, "B")
            .Task(4, "Sentinel")
            .Connect(JobGraph.EventId, 2)
            .Connect(JobGraph.EventId, 4)
            .Connect(2, 3, runMode: "OnceWithAllResults");
        using EngineHarness h = new(graph);

        h.Fire();
        Assert.IsTrue(EngineHarness.WaitFor(() => Runs("Sentinel").Count == 1, Timeout), "The run did not happen.");

        Thread.Sleep(300);
        Assert.IsEmpty(Runs("B"));
    }

    [TestMethod]
    public void The_next_task_iterates_over_the_rows_of_all_the_iterations_through_the_default_recordset()
    {
        // 3 iterations x 2 rows each: B sees one default recordset with all 6 rows, in order.
        JobGraph graph = new JobGraph()
            .IteratingTask(2, "A", iterations: 3, rowsPerIteration: 2)
            .IteratingTask(3, "B", iterationMode: "IterateDefaultRecordset",
                           value: "{object[2].DefaultRecordset[{iterationIndex}]['Iteration']}.{object[2].DefaultRecordset[{iterationIndex}]['Row']}")
            .Connect(JobGraph.EventId, 2)
            .Connect(2, 3, runMode: "OnceWithAllResults");
        using EngineHarness h = new(graph);

        h.Fire();
        Assert.IsTrue(EngineHarness.WaitFor(() => Runs("B#5").Count == 1, Timeout), "B did not iterate over all 6 rows.");

        string[] values = [.. Enumerable.Range(0, 6).Select(i => Runs($"B#{i}").Single().Value)];
        CollectionAssert.AreEqual(new[] { "0.0", "0.1", "1.0", "1.1", "2.0", "2.1" }, values);
        Assert.IsEmpty(Runs("B#6"));
    }

    [TestMethod]
    public void An_iteration_object_without_braces_fails_the_task_instead_of_running_it_once()
    {
        // Regression: "object[2].Results" (no braces) silently ran B once, on the first row only.
        JobGraph graph = new JobGraph()
            .IteratingTask(2, "A", iterations: 3)
            .IteratingTask(3, "B", iterationMode: "IterateObjectRecordset", iterationObject: "object[2].Results")
            .Task(4, "OnBFailure")
            .Connect(JobGraph.EventId, 2)
            .Connect(2, 3, runMode: "OnceWithAllResults")
            .Connect(3, 4, executeOperators: ["ObjectDoesNotExecute"]);
        using EngineHarness h = new(graph);

        h.Fire();
        Assert.IsTrue(EngineHarness.WaitFor(() => Runs("OnBFailure").Count == 1, Timeout), "B should fail.");
        Assert.IsEmpty(Runs("B#0"), "B must not run any iteration.");
    }

    [TestMethod]
    public void The_next_task_can_iterate_over_the_collected_results()
    {
        // Collect, then iterate: B runs once, and iterates over A's Results itself.
        JobGraph graph = new JobGraph()
            .IteratingTask(2, "A", iterations: 3)
            .IteratingTask(3, "B", iterationMode: "IterateObjectRecordset", iterationObject: "{object[2].Results}",
                           value: "{object[2].Results[{iterationIndex}]['Iteration']}")
            .Connect(JobGraph.EventId, 2)
            .Connect(2, 3, runMode: "OnceWithAllResults");
        using EngineHarness h = new(graph);

        h.Fire();
        Assert.IsTrue(EngineHarness.WaitFor(() => Runs("B#2").Count == 1, Timeout), "B did not iterate over the 3 results.");

        Assert.AreEqual("0", Runs("B#0").Single().Value);
        Assert.AreEqual("1", Runs("B#1").Single().Value);
        Assert.AreEqual("2", Runs("B#2").Single().Value);
        Assert.IsEmpty(Runs("B#3"));
    }
}
