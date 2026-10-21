// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging.Abstract;

namespace OSRobot.Server.JobEngineLib;

/// <summary>One task execution waiting in the <see cref="TaskQueue"/>, with everything it needs to run.</summary>
internal sealed record TaskWorkItem(JobRun Run, ITask Task, DynamicDataChain DataChain, DynamicDataSet LastDynamicDataSet,
                                    int? SubInstanceIndex, IPluginInstanceLogger Logger);

/// <summary>
/// Everything one trigger (an event occurrence or a manual start) causes to run: the first task(s) and,
/// transitively, every task their connections dispatch. Tracks how many of its work items are still
/// pending - queued, waiting out a connection's WaitSeconds, or running - and completes when none are left.
/// </summary>
internal sealed class JobRun
{
    private readonly CancellationTokenSource _cts;
    private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

    // Starts at 1 for the trigger itself, so the run can't complete while the trigger is still
    // dispatching its first tasks. The trigger releases it with CompleteItem() once done.
    private int _pendingItems = 1;

    public JobRun(long id, TaskQueue queue, CancellationToken callerToken)
    {
        Id = id;
        Queue = queue;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(queue.Token, callerToken);
    }

    public long Id { get; }

    /// <summary>The queue this run's work goes to. Fixed at creation, so work from a run started before a reload never lands in the new queue.</summary>
    public TaskQueue Queue { get; }

    /// <summary>Cancelled when the engine stops, or when the caller that started the run gives up (serial manual starts only).</summary>
    public CancellationToken Token => _cts.Token;

    public Task Completion => _completion.Task;

    public void AddItem() => Interlocked.Increment(ref _pendingItems);

    public void CompleteItem()
    {
        if (Interlocked.Decrement(ref _pendingItems) != 0)
            return;

        // Nothing of this run is left that could still observe the token.
        _cts.Dispose();
        _completion.TrySetResult();
    }
}
