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
}
