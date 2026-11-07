// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging.Abstract;

namespace OSRobot.Tests.Support;

/// <summary>
/// An event sink for testing an event plugin on its own, without a job engine. Records when each
/// occurrence was published, and lets a test await the Nth one without blocking a thread: the event
/// plugins publish from thread-pool callbacks (timers), which a blocked test thread would starve
/// when many tests run in parallel.
/// </summary>
public sealed class RecordingEventSink : IEventSink
{
    private readonly object _gate = new();
    private readonly List<DateTime> _occurrences = [];
    private readonly List<(int Count, TaskCompletionSource<DateTime> Tcs)> _waiters = [];
    private readonly List<DynamicDataSet> _published = [];
    private readonly List<(Func<DynamicDataSet, bool> Predicate, TaskCompletionSource<DynamicDataSet> Tcs)> _dataWaiters = [];

    public int Count { get { lock (_gate) return _occurrences.Count; } }

    /// <summary>The occurrences as seconds after <paramref name="start"/>, e.g. "5.0s, 10.0s", for assertion messages.</summary>
    public string Describe(DateTime start)
    {
        lock (_gate)
            return _occurrences.Count == 0 ? "none" : string.Join(", ", _occurrences.Select(o => $"{(o - start).TotalSeconds:F1}s"));
    }

    public bool Publish(IEvent source, DynamicDataSet dynamicData, IPluginInstanceLogger logger)
    {
        DateTime now = DateTime.Now;

        lock (_gate)
        {
            _occurrences.Add(now);

            foreach ((int count, TaskCompletionSource<DateTime> tcs) in _waiters.Where(w => w.Count == _occurrences.Count))
                tcs.TrySetResult(now);

            _published.Add(dynamicData);
            foreach ((Func<DynamicDataSet, bool> predicate, TaskCompletionSource<DynamicDataSet> tcs) in _dataWaiters.Where(w => w.Predicate(dynamicData)))
                tcs.TrySetResult(dynamicData);
        }

        return true;
    }

    /// <summary>Waits until the event has occurred <paramref name="count"/> times.</summary>
    /// <returns>When the count-th occurrence was published, or null if it didn't happen within <paramref name="timeout"/>.</returns>
    public async Task<DateTime?> WaitForOccurrenceAsync(int count, TimeSpan timeout)
    {
        // Completed by Publish under the lock: let the awaiting test continue elsewhere.
        TaskCompletionSource<DateTime> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        lock (_gate)
        {
            if (_occurrences.Count >= count)
                return _occurrences[count - 1];

            _waiters.Add((count, tcs));
        }

        try
        {
            return await tcs.Task.WaitAsync(timeout);
        }
        catch (TimeoutException)
        {
            return null;
        }
    }

    /// <summary>Waits until an occurrence whose dynamic data satisfies <paramref name="predicate"/> is published.</summary>
    /// <returns>That occurrence's dynamic data, or null if none came within <paramref name="timeout"/>.</returns>
    public async Task<DynamicDataSet?> WaitForPublishedAsync(Func<DynamicDataSet, bool> predicate, TimeSpan timeout)
    {
        TaskCompletionSource<DynamicDataSet> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        lock (_gate)
        {
            DynamicDataSet? published = _published.FirstOrDefault(predicate);
            if (published != null)
                return published;

            _dataWaiters.Add((predicate, tcs));
        }

        try
        {
            return await tcs.Task.WaitAsync(timeout);
        }
        catch (TimeoutException)
        {
            return null;
        }
    }
}
