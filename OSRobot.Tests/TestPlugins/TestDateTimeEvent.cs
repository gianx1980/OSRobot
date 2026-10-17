// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Plugins.DateTimeEvent;
using OSRobot.Tests.Support;

namespace OSRobot.Tests.TestPlugins;

/// <summary>
/// The DateTimeEvent with real timers, over a few seconds. The schedule itself (weekdays, daylight
/// saving changes, ...) is covered without waiting by <see cref="TestDateTimeEventScheduler"/>.
/// </summary>
[TestClass]
public sealed class TestDateTimeEvent
{
    private const double ToleranceSec = 1;

    // How much longer than expected (plus tolerance) to keep waiting for an occurrence. A late
    // occurrence then fails the timing assertion with its actual time, instead of looking like
    // one that never happened.
    private const int LateOccurrenceMarginSec = 10;

    private static DateTimeEvent CreateEvent(DateTimeEventConfig config)
    {
        config.Id = 1;
        config.Name = "DateTimeEvent 1";

        return new DateTimeEvent
        {
            ParentFolder = Common.CreateRootFolder(),
            Config = config
        };
    }

    private static void AssertOccurredAt(DateTime expected, DateTime? actual, RecordingEventSink sink, DateTime start)
    {
        Assert.IsNotNull(actual, $"The event did not occur as many times as expected. Occurrences: {sink.Describe(start)}.");
        Assert.IsLessThanOrEqualTo(ToleranceSec, Math.Abs((actual.Value - expected).TotalSeconds),
            $"The event did not occur at the expected time ({(expected - start).TotalSeconds:F1}s). Occurrences: {sink.Describe(start)}.");
    }

    [TestMethod]
    public async Task TestOneTime()
    {
        DateTimeEventConfig config = new()
        {
            Schedule = DateTimeEventSchedule.OneTime,
            AtDate = DateTime.Now.AddSeconds(4)
        };
        DateTimeEvent eventObj = CreateEvent(config);
        RecordingEventSink sink = new();

        try
        {
            DateTime start = DateTime.Now;
            eventObj.Init(sink);

            DateTime? triggeredAt = await sink.WaitForOccurrenceAsync(1, TimeSpan.FromSeconds(4 + ToleranceSec + LateOccurrenceMarginSec));
            AssertOccurredAt(config.AtDate, triggeredAt, sink, start);

            // Once only.
            DateTime? second = await sink.WaitForOccurrenceAsync(2, TimeSpan.FromSeconds(3));
            Assert.IsNull(second, $"A one time event occurred more than once: {sink.Describe(start)}.");
        }
        finally
        {
            eventObj.Destroy();
        }
    }

    [TestMethod]
    public async Task TestOneTimeInThePastNeverOccurs()
    {
        DateTimeEventConfig config = new()
        {
            Schedule = DateTimeEventSchedule.OneTime,
            AtDate = DateTime.Now.AddSeconds(-2)
        };
        DateTimeEvent eventObj = CreateEvent(config);
        RecordingEventSink sink = new();

        try
        {
            eventObj.Init(sink);

            Assert.IsNull(await sink.WaitForOccurrenceAsync(1, TimeSpan.FromSeconds(3)), "A missed one time occurrence must be skipped, not fired at startup.");
        }
        finally
        {
            eventObj.Destroy();
        }
    }

    [TestMethod]
    public async Task TestEveryDayAt()
    {
        DateTime now = DateTime.Now;
        TimeSpan timeOfDay = TimeSpan.FromSeconds(Math.Floor(now.AddSeconds(4).TimeOfDay.TotalSeconds));
        DateTime expected = now.Date + timeOfDay;
        if (expected <= now)
            expected = expected.AddDays(1);   // Just before midnight: the time is tomorrow's.

        DateTimeEventConfig config = new()
        {
            Schedule = DateTimeEventSchedule.EveryDayAt,
            DailyTime = timeOfDay.ToString(@"hh\:mm\:ss")
        };
        DateTimeEvent eventObj = CreateEvent(config);
        RecordingEventSink sink = new();

        try
        {
            DateTime start = DateTime.Now;
            eventObj.Init(sink);

            DateTime? triggeredAt = await sink.WaitForOccurrenceAsync(1, TimeSpan.FromSeconds(5 + ToleranceSec + LateOccurrenceMarginSec));
            AssertOccurredAt(expected, triggeredAt, sink, start);
        }
        finally
        {
            eventObj.Destroy();
        }
    }

    [TestMethod]
    public async Task TestIntervalAlignedToAtDate()
    {
        // Simulates a restart long after AtDate: the occurrences follow the AtDate-based schedule
        // (AtDate + N * interval), not the moment the event is initialized.
        int intervalSec = 2;
        int repeatNumber = 3;

        DateTimeEventConfig config = new()
        {
            Schedule = DateTimeEventSchedule.Interval,
            AtDate = DateTime.Now.AddSeconds(-7.3),
            IntervalHours = 0,
            IntervalMinutes = 0,
            IntervalSeconds = intervalSec
        };
        DateTimeEvent eventObj = CreateEvent(config);
        RecordingEventSink sink = new();

        try
        {
            DateTime start = DateTime.Now;
            long firstPeriod = (long)Math.Floor((start - config.AtDate).TotalSeconds / intervalSec) + 1;
            DateTime expectedLast = config.AtDate.AddSeconds(intervalSec * (firstPeriod + repeatNumber - 1));
            eventObj.Init(sink);

            DateTime? lastTriggeredAt = await sink.WaitForOccurrenceAsync(repeatNumber, TimeSpan.FromSeconds((intervalSec * repeatNumber) + ToleranceSec + LateOccurrenceMarginSec));
            AssertOccurredAt(expectedLast, lastTriggeredAt, sink, start);
        }
        finally
        {
            eventObj.Destroy();
        }
    }

    [TestMethod]
    public async Task TestIntervalOnTodaysWeekday()
    {
        DateTimeEventConfig config = new()
        {
            Schedule = DateTimeEventSchedule.Interval,
            AtDate = DateTime.Now.AddSeconds(-10),
            IntervalMinutes = 0,
            IntervalSeconds = 1,
            OnDays = [DateTime.Now.DayOfWeek]
        };
        DateTimeEvent eventObj = CreateEvent(config);
        RecordingEventSink sink = new();

        try
        {
            eventObj.Init(sink);

            Assert.IsNotNull(await sink.WaitForOccurrenceAsync(1, TimeSpan.FromSeconds(1 + ToleranceSec + LateOccurrenceMarginSec)), "The event did not occur on a day it is configured for.");
        }
        finally
        {
            eventObj.Destroy();
        }
    }

    [TestMethod]
    public async Task TestIntervalNotOnTodaysWeekday()
    {
        DateTimeEventConfig config = new()
        {
            Schedule = DateTimeEventSchedule.Interval,
            AtDate = DateTime.Now.AddSeconds(-10),
            IntervalMinutes = 0,
            IntervalSeconds = 1,
            OnDays = [(DayOfWeek)(((int)DateTime.Now.DayOfWeek + 1) % 7)]
        };
        DateTimeEvent eventObj = CreateEvent(config);
        RecordingEventSink sink = new();

        try
        {
            eventObj.Init(sink);

            Assert.IsNull(await sink.WaitForOccurrenceAsync(1, TimeSpan.FromSeconds(3)), "The event must not occur on a day it is not configured for.");
        }
        finally
        {
            eventObj.Destroy();
        }
    }

    [TestMethod]
    public async Task TestDestroyStopsTheEvent()
    {
        DateTimeEventConfig config = new()
        {
            Schedule = DateTimeEventSchedule.Interval,
            AtDate = DateTime.Now,
            IntervalMinutes = 0,
            IntervalSeconds = 1
        };
        DateTimeEvent eventObj = CreateEvent(config);
        RecordingEventSink sink = new();

        eventObj.Init(sink);
        Assert.IsNotNull(await sink.WaitForOccurrenceAsync(1, TimeSpan.FromSeconds(1 + ToleranceSec + LateOccurrenceMarginSec)), "The event did not occur.");

        eventObj.Destroy();
        int countAtDestroy = sink.Count;
        await Task.Delay(TimeSpan.FromSeconds(2.5));

        Assert.AreEqual(countAtDestroy, sink.Count, "The event occurred after Destroy().");
    }

    [TestMethod]
    public async Task TestInvalidConfigurationDoesNotThrowAndNeverOccurs()
    {
        DateTimeEventConfig config = new()
        {
            Schedule = DateTimeEventSchedule.Interval,
            IntervalHours = 0,
            IntervalMinutes = 0,
            IntervalSeconds = 0
        };
        DateTimeEvent eventObj = CreateEvent(config);
        RecordingEventSink sink = new();

        try
        {
            // The engine initializes all the events in one go: one bad event must not stop the others.
            eventObj.Init(sink);

            Assert.IsNull(await sink.WaitForOccurrenceAsync(1, TimeSpan.FromSeconds(2)));
        }
        finally
        {
            eventObj.Destroy();
        }
    }
}
