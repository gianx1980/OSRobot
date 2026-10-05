// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging.Abstract;

namespace OSRobot.Tests.TestClasses;

public sealed class CapturingInstanceLogger : IPluginInstanceLogger
{
    private readonly object _gate = new();
    private readonly List<string> _errors = [];

    public IReadOnlyList<string> Errors { get { lock (_gate) return [.. _errors]; } }

    private void AddError(string text) { lock (_gate) _errors.Add(text); }

    public void Init(string pathFileName) { }
    public void TaskStarting(ITask task) { }
    public void TaskStarted(ITask task) { }
    public void TaskCompleted(ITask task) { }
    public void TaskError(ITask task, Exception ex) => AddError($"TaskError: {ex.Message}");
    public void TaskEnded(ITask task) { }
    public void TaskIterationError(ITask task, int iterationIndex, Exception ex) => AddError($"TaskIterationError[{iterationIndex}]: {ex.Message}");
    public void EventError(IEvent tdrEvent, Exception ex) => AddError($"EventError: {ex.Message}");
    public void EventTriggering(IEvent tdrEvent) { }
    public void EventTriggered(IEvent tdrEvent) { }
    public void Info(string text) { }
    public void Info(string text, Exception ex) { }
    public void Info(IPluginInstance pluginInstance, string text) { }
    public void Info(IPluginInstance pluginInstance, string text, Exception ex) { }
    public void Error(string text) => AddError(text);
    public void Error(string text, Exception ex) => AddError($"{text}: {ex.Message}");
    public void Error(IPluginInstance pluginInstance, string text) => AddError(text);
    public void Error(IPluginInstance pluginInstance, string text, Exception ex) => AddError($"{text}: {ex.Message}");
}

public class ScriptedTaskConfig : ITaskConfig
{
    public int Id { get; set; } = 1;
    public string Name { get; set; } = "Scripted";
    public bool Enabled { get; set; } = true;
    public bool Log { get; set; }
    public IterationMode PluginIterationMode { get; set; } = IterationMode.IterateExactNumber;
    public string IterationObject { get; set; } = string.Empty;
    public int IterationsCount { get; set; } = 1;

    public int FailOnIteration { get; set; } = -1;
    public int CancelOnIteration { get; set; } = -1;
    public bool ThrowInPostTaskSucceded { get; set; }
}

public class ScriptedMultipleIterationTask : MultipleIterationTask
{
    public CancellationTokenSource Cts { get; } = new();
    public List<int> ExecutedIterations { get; } = [];

    protected override Task RunMultipleIterationTaskAsync(int currentIteration)
    {
        ScriptedTaskConfig config = (ScriptedTaskConfig)_iterationTaskConfig;
        ExecutedIterations.Add(currentIteration);

        if (currentIteration == config.CancelOnIteration)
        {
            Cts.Cancel();
            _cancellationToken.ThrowIfCancellationRequested();
        }

        if (currentIteration == config.FailOnIteration)
            throw new InvalidOperationException($"Iteration {currentIteration} failed.");

        return Task.CompletedTask;
    }

    protected override void PostTaskSucceded(int currentIteration, ExecResult result, DynamicDataSet dDataSet)
    {
        if (((ScriptedTaskConfig)Config).ThrowInPostTaskSucceded)
            throw new InvalidOperationException("PostTaskSucceded failed.");
    }
}

public class ScriptedSingleIterationTask : SingleIterationTask
{
    public CancellationTokenSource Cts { get; } = new();

    protected override Task RunSingleIterationTaskAsync()
    {
        ScriptedTaskConfig config = (ScriptedTaskConfig)_taskConfig;

        if (config.CancelOnIteration == 0)
        {
            Cts.Cancel();
            _cancellationToken.ThrowIfCancellationRequested();
        }

        if (config.FailOnIteration == 0)
            throw new InvalidOperationException("Task failed.");

        return Task.CompletedTask;
    }
}

[TestClass]
public sealed class TestTaskErrorHandling
{
    private static Task<InstanceExecResult> Run(BaseTask task, ScriptedTaskConfig config, IPluginInstanceLogger logger, CancellationToken cancellationToken)
    {
        task.Config = config;
        return task.RunAsync(new DynamicDataChain(), new DynamicDataSet(), null, logger, cancellationToken);
    }

    private static bool[] Outcomes(InstanceExecResult result) => [.. result.ExecResults.Select(r => r.Result)];

    [TestMethod]
    public async Task A_failed_iteration_yields_one_failed_result_and_the_next_iterations_still_run()
    {
        CapturingInstanceLogger logger = new();
        ScriptedMultipleIterationTask task = new();

        InstanceExecResult result = await Run(task, new ScriptedTaskConfig { IterationsCount = 3, FailOnIteration = 1 }, logger, task.Cts.Token);

        CollectionAssert.AreEqual(new[] { true, false, true }, Outcomes(result));
        CollectionAssert.AreEqual(new[] { 0, 1, 2 }, task.ExecutedIterations);
        CollectionAssert.AreEqual(new[] { "TaskIterationError[1]: Iteration 1 failed." }, logger.Errors.ToArray());
    }

    [TestMethod]
    public async Task A_single_iteration_failure_yields_a_failed_result_instead_of_throwing()
    {
        CapturingInstanceLogger logger = new();
        ScriptedSingleIterationTask task = new();

        InstanceExecResult result = await Run(task, new ScriptedTaskConfig { FailOnIteration = 0 }, logger, task.Cts.Token);

        CollectionAssert.AreEqual(new[] { false }, Outcomes(result));
        CollectionAssert.AreEqual(new[] { "TaskError: Task failed." }, logger.Errors.ToArray());
    }

    [TestMethod]
    public async Task Errors_are_logged_even_when_Log_is_off()
    {
        CapturingInstanceLogger logger = new();
        ScriptedSingleIterationTask task = new();

        await Run(task, new ScriptedTaskConfig { Log = false, FailOnIteration = 0 }, logger, task.Cts.Token);

        Assert.AreEqual(1, logger.Errors.Count);
    }

    [TestMethod]
    public async Task A_throwing_PostTaskSucceded_hook_yields_exactly_one_failed_result_per_iteration()
    {
        CapturingInstanceLogger logger = new();
        ScriptedMultipleIterationTask task = new();

        InstanceExecResult result = await Run(task, new ScriptedTaskConfig { IterationsCount = 2, ThrowInPostTaskSucceded = true }, logger, task.Cts.Token);

        CollectionAssert.AreEqual(new[] { false, false }, Outcomes(result));
    }

    [TestMethod]
    public async Task Cancellation_stops_the_iterations_and_propagates_instead_of_failing_them()
    {
        CapturingInstanceLogger logger = new();
        ScriptedMultipleIterationTask task = new();
        ScriptedTaskConfig config = new() { IterationsCount = 5, CancelOnIteration = 1 };

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => Run(task, config, logger, task.Cts.Token));

        CollectionAssert.AreEqual(new[] { 0, 1 }, task.ExecutedIterations);
        Assert.AreEqual(0, logger.Errors.Count);
    }

    [TestMethod]
    public async Task Cancellation_of_a_single_iteration_task_propagates_without_a_failed_result()
    {
        CapturingInstanceLogger logger = new();
        ScriptedSingleIterationTask task = new();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => Run(task, new ScriptedTaskConfig { CancelOnIteration = 0 }, logger, task.Cts.Token));

        Assert.AreEqual(0, logger.Errors.Count);
    }
}
