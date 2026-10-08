// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Plugins.DateTimeEvent;
using OSRobot.Tests.Support;

namespace OSRobot.Tests.TestPlugins;

[TestClass]
public sealed class TestDateTimeEvent
{
    // How much longer than expected (plus tolerance) to keep waiting for an occurrence. A late
    // occurrence then fails the timing assertion with its actual time, instead of looking like
    // one that never happened.
    private const int LateOccurrenceMarginSec = 10;

    [TestMethod]
    public async Task TestAtTime()
    {
        // ---------
        // Arrange
        // ---------
        int withinMinutes = 1;
        int toleranceSec = 1;
        Folder folder = Common.CreateRootFolder();

        DateTimeEventConfig config = new()
        {
            Id = 1,
            Name = "DateTimeEvent 1",
            OneTime = true,
            AtDate = DateTime.Now.AddMinutes(withinMinutes)
        };

        DateTimeEvent eventObj = new()
        {
            ParentFolder = folder,
            Config = config
        };

        RecordingEventSink sink = new();

        try
        {
            // ---------
            // Act
            // ---------
            DateTime start = DateTime.Now;
            eventObj.Init(sink);

            DateTime? triggeredAt = await sink.WaitForOccurrenceAsync(1, new TimeSpan(0, withinMinutes, toleranceSec + LateOccurrenceMarginSec));

            // ---------
            // Assert
            // ---------
            Assert.IsNotNull(triggeredAt, "The event did not occur.");
            Assert.IsLessThanOrEqualTo(toleranceSec, Math.Abs(triggeredAt.Value.Subtract(config.AtDate).TotalSeconds), $"The event did not occur at the expected time. Occurrences: {sink.Describe(start)}.");
        }
        finally
        {
            eventObj.Destroy();
        }
    }

    [TestMethod]
    public async Task TestEverySecond()
    {
        // ---------
        // Arrange
        // ---------
        int everyNumSeconds = 5;
        int repeatNumber = 5;
        int toleranceSec = 1;
        Folder folder = Common.CreateRootFolder();

        DateTimeEventConfig config = new()
        {
            Id = 1,
            Name = "DateTimeEvent 1",
            EverySeconds = true,
            AtDate = DateTime.Now.AddSeconds(-10), // Start with AtTime in the past
            EveryNumSeconds = everyNumSeconds
        };

        DateTimeEvent eventObj = new()
        {
            ParentFolder = folder,
            Config = config
        };

        RecordingEventSink sink = new();

        try
        {
            // ---------
            // Act
            // ---------
            DateTime start = DateTime.Now;
            DateTime expectedLastTrigger = start.AddSeconds(everyNumSeconds * repeatNumber);
            eventObj.Init(sink);

            DateTime? lastTriggeredAt = await sink.WaitForOccurrenceAsync(repeatNumber, new TimeSpan(0, 0, (everyNumSeconds * repeatNumber) + toleranceSec + LateOccurrenceMarginSec));

            // ---------
            // Assert
            // ---------
            Assert.IsNotNull(lastTriggeredAt, $"The event occurred {sink.Count} time(s) instead of {repeatNumber}, at: {sink.Describe(start)}.");
            Assert.IsLessThanOrEqualTo(toleranceSec, Math.Abs(lastTriggeredAt.Value.Subtract(expectedLastTrigger).TotalSeconds), $"The event did not occur at the expected time. Occurrences: {sink.Describe(start)}.");
        }
        finally
        {
            eventObj.Destroy();
        }
    }

    [TestMethod]
    public async Task TestEveryMinute()
    {
        // ---------
        // Arrange
        // ---------
        int everyNumMinutes = 1;
        int repeatNumber = 5;
        int toleranceSec = 1;
        Folder folder = Common.CreateRootFolder();

        DateTimeEventConfig config = new()
        {
            Id = 1,
            Name = "DateTimeEvent 1",
            EveryDaysHoursSecs = true,
            AtDate = DateTime.Now.AddSeconds(-10), // Start with AtTime in the past
            EveryNumDays = 0,
            EveryNumHours = 0,
            EveryNumMinutes = everyNumMinutes
        };

        DateTimeEvent eventObj = new()
        {
            ParentFolder = folder,
            Config = config
        };

        RecordingEventSink sink = new();

        try
        {
            // ---------
            // Act
            // ---------
            DateTime start = DateTime.Now;
            DateTime expectedLastTrigger = start.AddMinutes(everyNumMinutes * repeatNumber);
            eventObj.Init(sink);

            DateTime? lastTriggeredAt = await sink.WaitForOccurrenceAsync(repeatNumber, new TimeSpan(0, 0, (everyNumMinutes * 60 * repeatNumber) + toleranceSec + LateOccurrenceMarginSec));

            // ---------
            // Assert
            // ---------
            Assert.IsNotNull(lastTriggeredAt, $"The event occurred {sink.Count} time(s) instead of {repeatNumber}, at: {sink.Describe(start)}.");
            Assert.IsLessThanOrEqualTo(toleranceSec, Math.Abs(lastTriggeredAt.Value.Subtract(expectedLastTrigger).TotalSeconds), $"The event did not occur at the expected time. Occurrences: {sink.Describe(start)}.");
        }
        finally
        {
            eventObj.Destroy();
        }
    }

    [TestMethod]
    public async Task TestEverySecondOnDaysTrue()
    {
        // ---------
        // Arrange
        // ---------
        int everyNumSeconds = 5;
        int repeatNumber = 1;
        int toleranceSec = 1;
        Folder folder = Common.CreateRootFolder();

        DateTimeEventConfig config = new()
        {
            Id = 1,
            Name = "DateTimeEvent 1",
            EverySeconds = true,
            AtDate = DateTime.Now.AddSeconds(-10), // Start with AtTime in the past
            EveryNumSeconds = everyNumSeconds
        };
        config.OnDays.Clear();
        config.OnDays.Add(DateTime.Now.DayOfWeek);

        DateTimeEvent eventObj = new()
        {
            ParentFolder = folder,
            Config = config
        };

        RecordingEventSink sink = new();

        try
        {
            // ---------
            // Act
            // ---------
            DateTime start = DateTime.Now;
            DateTime expectedLastTrigger = start.AddSeconds(everyNumSeconds * repeatNumber);
            eventObj.Init(sink);

            DateTime? lastTriggeredAt = await sink.WaitForOccurrenceAsync(repeatNumber, new TimeSpan(0, 0, (everyNumSeconds * repeatNumber) + toleranceSec + LateOccurrenceMarginSec));

            // ---------
            // Assert
            // ---------
            Assert.IsNotNull(lastTriggeredAt, $"The event did not occur. Occurrences: {sink.Describe(start)}.");
            Assert.IsLessThanOrEqualTo(toleranceSec, Math.Abs(lastTriggeredAt.Value.Subtract(expectedLastTrigger).TotalSeconds), $"The event did not occur at the expected time. Occurrences: {sink.Describe(start)}.");
        }
        finally
        {
            eventObj.Destroy();
        }
    }

    [TestMethod]
    public async Task TestEverySecondOnAllDays()
    {
        // ---------
        // Arrange
        // ---------
        int everyNumSeconds = 5;
        int repeatNumber = 1;
        int toleranceSec = 1;
        Folder folder = Common.CreateRootFolder();

        DateTimeEventConfig config = new()
        {
            Id = 1,
            Name = "DateTimeEvent 1",
            EverySeconds = true,
            AtDate = DateTime.Now.AddSeconds(-10), // Start with AtTime in the past
            EveryNumSeconds = everyNumSeconds
        };
        config.OnDays.Clear();
        config.OnAllDays = true;

        DateTimeEvent eventObj = new()
        {
            ParentFolder = folder,
            Config = config
        };

        RecordingEventSink sink = new();

        try
        {
            // ---------
            // Act
            // ---------
            DateTime start = DateTime.Now;
            DateTime expectedLastTrigger = start.AddSeconds(everyNumSeconds * repeatNumber);
            eventObj.Init(sink);

            DateTime? lastTriggeredAt = await sink.WaitForOccurrenceAsync(repeatNumber, new TimeSpan(0, 0, (everyNumSeconds * repeatNumber) + toleranceSec + LateOccurrenceMarginSec));

            // ---------
            // Assert
            // ---------
            Assert.IsNotNull(lastTriggeredAt, $"The event did not occur. Occurrences: {sink.Describe(start)}.");
            Assert.IsLessThanOrEqualTo(toleranceSec, Math.Abs(lastTriggeredAt.Value.Subtract(expectedLastTrigger).TotalSeconds), $"The event did not occur at the expected time. Occurrences: {sink.Describe(start)}.");
        }
        finally
        {
            eventObj.Destroy();
        }
    }

    [TestMethod]
    public async Task TestEverySecondOnDaysFalse()
    {
        // ---------
        // Arrange
        // ---------
        int everyNumSeconds = 5;
        int repeatNumber = 1;
        int toleranceSec = 1;
        Folder folder = Common.CreateRootFolder();

        DateTimeEventConfig config = new()
        {
            Id = 1,
            Name = "DateTimeEvent 1",
            EverySeconds = true,
            AtDate = DateTime.Now.AddSeconds(-10), // Start with AtTime in the past
            EveryNumSeconds = everyNumSeconds
        };
        config.OnDays.Clear();
        config.OnAllDays = false;
        foreach (DayOfWeek D in Enum.GetValues(typeof(DayOfWeek)))
        {
            if (D != DateTime.Now.DayOfWeek)
            {
                config.OnDays.Add(D);
                break;
            }
        }

        DateTimeEvent eventObj = new()
        {
            ParentFolder = folder,
            Config = config
        };

        RecordingEventSink sink = new();

        try
        {
            // ---------
            // Act
            // ---------
            eventObj.Init(sink);

            DateTime? triggeredAt = await sink.WaitForOccurrenceAsync(repeatNumber, new TimeSpan(0, 0, (everyNumSeconds * repeatNumber) + toleranceSec));

            // ---------
            // Assert
            // ---------
            Assert.IsNull(triggeredAt, "The event must not occur on a day it is not configured for.");
        }
        finally
        {
            eventObj.Destroy();
        }
    }
}
