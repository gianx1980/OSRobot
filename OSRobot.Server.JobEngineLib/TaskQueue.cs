// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Threading.Channels;
using OSRobot.Server.Core.Logging.Abstract;

namespace OSRobot.Server.JobEngineLib;

/// <summary>
/// The engine's work queue for one Start()/Stop() cycle: a FIFO of task executions consumed by a fixed
/// number of workers, which is what bounds how many tasks run at the same time. Workers never wait on
/// each other - a finished task enqueues its successors instead of running them - so any worker count,
/// including 1, can make progress.
/// </summary>
internal sealed class TaskQueue
{
    private readonly Channel<TaskWorkItem> _channel = Channel.CreateUnbounded<TaskWorkItem>();
    private readonly CancellationTokenSource _cts = new();
    private readonly Func<TaskWorkItem, Task> _processItem;
    private readonly IAppLogger _log;
    private readonly Task[] _workers;

    // Items enqueued and not yet fully processed (queued + running).
    private int _pendingCount;

    public TaskQueue(int workerCount, Func<TaskWorkItem, Task> processItem, IAppLogger log)
    {
        _processItem = processItem;
        _log = log;
        _workers = [.. Enumerable.Range(0, Math.Max(1, workerCount)).Select(_ => Task.Run(WorkerLoopAsync))];
    }

    /// <summary>Cancelled by <see cref="Stop"/>: everything running is asked to stop, everything still queued is skipped.</summary>
    public CancellationToken Token => _cts.Token;

    public int PendingCount => Volatile.Read(ref _pendingCount);

    /// <returns>false once <see cref="Stop"/> has been called: the item is not queued.</returns>
    public bool TryEnqueue(TaskWorkItem item)
    {
        Interlocked.Increment(ref _pendingCount);
        if (_channel.Writer.TryWrite(item))
            return true;

        Interlocked.Decrement(ref _pendingCount);
        return false;
    }

    private async Task WorkerLoopAsync()
    {
        // No cancellation token here on purpose: after Stop() the workers keep reading until the queue
        // is empty, so every queued item still gets processed (as a skip) and its run can complete.
        await foreach (TaskWorkItem item in _channel.Reader.ReadAllAsync())
        {
            try
            {
                await _processItem(item);
            }
            catch (Exception ex)
            {
                // Safety net: processItem handles its own failures. A worker must never die, or the
                // engine would silently lose capacity.
                _log.Error("Unhandled error processing a queued task", ex);
            }
            finally
            {
                Interlocked.Decrement(ref _pendingCount);
            }
        }
    }

    /// <summary>
    /// Stops accepting work, cancels what is running, and waits - up to <paramref name="timeout"/>, or until
    /// <paramref name="cancellationToken"/> is cancelled - for the workers to drain the queue.
    /// </summary>
    /// <returns>true if the workers finished; false if some work is still in flight.</returns>
    public bool Stop(TimeSpan timeout, CancellationToken cancellationToken)
    {
        _cts.Cancel();
        _channel.Writer.TryComplete();

        bool drained;
        try
        {
            drained = Task.WhenAll(_workers).Wait(timeout, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            drained = false;
        }

        // Left undisposed when work is still in flight: it may still be reading the token.
        if (drained)
            _cts.Dispose();

        return drained;
    }
}
