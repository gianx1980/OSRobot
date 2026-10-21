// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Plugins.DateTimeEvent;

namespace OSRobot.Tests.TestPlugins;

/// <summary>
/// The DateTimeEvent schedule, computed for fixed moments in a fixed time zone (no timers, no waiting).
/// Europe/Rome: CET (UTC+1) in winter, CEST (UTC+2) in summer. In 2026 clocks move forward on
/// 29 March at 02:00 (02:00-03:00 doesn't exist) and back on 25 October at 03:00 (02:00-03:00 happens twice).
/// </summary>
[TestClass]
public sealed class TestDateTimeEventScheduler
{
    private static readonly TimeZoneInfo _rome = FindRome();

    private static TimeZoneInfo FindRome()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
        }
    }

    private static DateTime Utc(int year, int month, int day, int hour, int minute = 0, int second = 0) =>
        new(year, month, day, hour, minute, second, DateTimeKind.Utc);

    private static DateTime Local(int year, int month, int day, int hour, int minute = 0, int second = 0) =>
        new(year, month, day, hour, minute, second, DateTimeKind.Unspecified);

    private static DateTime? Next(DateTimeEventConfig config, DateTime afterUtc) =>
        DateTimeEventScheduler.GetNextOccurrence(config, afterUtc, _rome);

    private static DateTimeEventConfig Daily(string time, params DayOfWeek[] onDays)
    {
        DateTimeEventConfig config = new() { Schedule = DateTimeEventSchedule.EveryDayAt, DailyTime = time };
        if (onDays.Length > 0)
            config.OnDays = [.. onDays];
        return config;
    }

    private static DateTimeEventConfig Interval(DateTime atDate, int hours, int minutes, int seconds) =>
        new() { Schedule = DateTimeEventSchedule.Interval, AtDate = atDate, IntervalHours = hours, IntervalMinutes = minutes, IntervalSeconds = seconds };

    // ---------------- One time ----------------

    [TestMethod]
    public void OneTime_InTheFuture_FiresAtAtDate()
    {
        DateTimeEventConfig config = new() { Schedule = DateTimeEventSchedule.OneTime, AtDate = Local(2026, 1, 15, 10, 30) };

        Assert.AreEqual(Utc(2026, 1, 15, 9, 30), Next(config, Utc(2026, 1, 15, 8, 0)));
    }

    [TestMethod]
    public void OneTime_InThePast_NeverFires()
    {
        DateTimeEventConfig config = new() { Schedule = DateTimeEventSchedule.OneTime, AtDate = Local(2026, 1, 15, 10, 30) };

        Assert.IsNull(Next(config, Utc(2026, 1, 15, 9, 30)));
    }

    [TestMethod]
    public void OneTime_IgnoresTheWeekdays()
    {
        // 15 January 2026 is a Thursday.
        DateTimeEventConfig config = new() { Schedule = DateTimeEventSchedule.OneTime, AtDate = Local(2026, 1, 15, 10, 30), OnDays = [DayOfWeek.Monday] };

        Assert.AreEqual(Utc(2026, 1, 15, 9, 30), Next(config, Utc(2026, 1, 15, 8, 0)));
    }

    [TestMethod]
    public void OneTime_LegacyUtcSuffix_IsTakenAsWallClock()
    {
        // Jobs saved by older editors have AtDate values like "2026-01-15T10:30:00.000Z" that mean 10:30 local.
        DateTimeEventConfig config = new() { Schedule = DateTimeEventSchedule.OneTime, AtDate = new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc) };

        Assert.AreEqual(Utc(2026, 1, 15, 9, 30), Next(config, Utc(2026, 1, 15, 8, 0)));
    }

    // ---------------- Every day at ----------------

    [TestMethod]
    public void EveryDayAt_BeforeTheTime_FiresToday()
    {
        Assert.AreEqual(Utc(2026, 1, 15, 1, 0), Next(Daily("02:00:00"), Utc(2026, 1, 15, 0, 30)));
    }

    [TestMethod]
    public void EveryDayAt_AfterTheTime_FiresTomorrow()
    {
        Assert.AreEqual(Utc(2026, 1, 16, 1, 0), Next(Daily("02:00:00"), Utc(2026, 1, 15, 1, 0)));
    }

    [TestMethod]
    public void EveryDayAt_KeepsTheSeconds()
    {
        Assert.AreEqual(Utc(2026, 1, 15, 22, 15, 42), Next(Daily("23:15:42"), Utc(2026, 1, 15, 12, 0)));
    }

    [TestMethod]
    public void EveryDayAt_AcceptsHoursAndMinutesOnly()
    {
        Assert.AreEqual(Utc(2026, 1, 15, 22, 15), Next(Daily("23:15"), Utc(2026, 1, 15, 12, 0)));
    }

    [TestMethod]
    public void EveryDayAt_SkipsTheWeekdaysNotSelected()
    {
        // Thursday 15 January, after 02:00: the next Monday is the 19th.
        Assert.AreEqual(Utc(2026, 1, 19, 1, 0), Next(Daily("02:00:00", DayOfWeek.Monday), Utc(2026, 1, 15, 12, 0)));
    }

    [TestMethod]
    public void EveryDayAt_OnlyTodaysWeekdayAndTimePassed_FiresNextWeek()
    {
        Assert.AreEqual(Utc(2026, 1, 22, 1, 0), Next(Daily("02:00:00", DayOfWeek.Thursday), Utc(2026, 1, 15, 12, 0)));
    }

    [TestMethod]
    public void EveryDayAt_NoWeekdaySelected_NeverFires()
    {
        DateTimeEventConfig config = Daily("02:00:00");
        config.OnDays = [];

        Assert.IsNull(Next(config, Utc(2026, 1, 15, 0, 0)));
    }

    [TestMethod]
    public void EveryDayAt_StaysAtTheSameWallClockTimeAcrossDaylightSavingChanges()
    {
        DateTimeEventConfig config = Daily("02:00:00");

        // 28 March: 02:00 CET. 30 March: 02:00 CEST. 26 October: 02:00 CET again.
        Assert.AreEqual(Utc(2026, 3, 28, 1, 0), Next(config, Utc(2026, 3, 27, 12, 0)));
        Assert.AreEqual(Utc(2026, 3, 30, 0, 0), Next(config, Utc(2026, 3, 29, 12, 0)));
        Assert.AreEqual(Utc(2026, 10, 26, 1, 0), Next(config, Utc(2026, 10, 25, 12, 0)));
    }

    [TestMethod]
    public void EveryDayAt_TimeSkippedByClocksMovingForward_FiresWhenTheClockJumps()
    {
        // 29 March: 02:30 doesn't exist, the clock goes from 02:00 CET to 03:00 CEST (01:00 UTC).
        DateTimeEventConfig config = Daily("02:30:00");

        Assert.AreEqual(Utc(2026, 3, 29, 1, 0), Next(config, Utc(2026, 3, 28, 12, 0)));
        Assert.AreEqual(Utc(2026, 3, 30, 0, 30), Next(config, Utc(2026, 3, 29, 1, 0)));
    }

    [TestMethod]
    public void EveryDayAt_TimeRepeatedByClocksMovingBack_FiresOnce()
    {
        // 25 October: 02:30 happens at 00:30 UTC (CEST) and again at 01:30 UTC (CET). It fires at the second.
        DateTimeEventConfig config = Daily("02:30:00");

        Assert.AreEqual(Utc(2026, 10, 25, 1, 30), Next(config, Utc(2026, 10, 24, 12, 0)));
        Assert.AreEqual(Utc(2026, 10, 25, 1, 30), Next(config, Utc(2026, 10, 25, 0, 30)));
        Assert.AreEqual(Utc(2026, 10, 26, 1, 30), Next(config, Utc(2026, 10, 25, 1, 30)));
    }

    // ---------------- Interval ----------------

    [TestMethod]
    public void Interval_AtDateInTheFuture_FirstOccurrenceIsAtDate()
    {
        Assert.AreEqual(Utc(2026, 1, 15, 9, 0), Next(Interval(Local(2026, 1, 15, 10, 0), 0, 15, 0), Utc(2026, 1, 15, 8, 0)));
    }

    [TestMethod]
    public void Interval_AtDateInThePast_StaysAlignedToAtDate()
    {
        // Every 15 minutes from 10:00 (09:00 UTC): after 09:37:10 UTC comes 09:45 UTC.
        DateTimeEventConfig config = Interval(Local(2026, 1, 15, 10, 0), 0, 15, 0);

        Assert.AreEqual(Utc(2026, 1, 15, 9, 45), Next(config, Utc(2026, 1, 15, 9, 37, 10)));
        Assert.AreEqual(Utc(2026, 1, 15, 10, 0), Next(config, Utc(2026, 1, 15, 9, 45)));
    }

    [TestMethod]
    public void Interval_CombinesHoursMinutesAndSeconds()
    {
        DateTimeEventConfig config = Interval(Local(2026, 1, 15, 10, 0), 1, 2, 3);

        Assert.AreEqual(Utc(2026, 1, 15, 10, 2, 3), Next(config, Utc(2026, 1, 15, 9, 0)));
        Assert.AreEqual(Utc(2026, 1, 15, 11, 4, 6), Next(config, Utc(2026, 1, 15, 10, 2, 3)));
    }

    [TestMethod]
    public void Interval_SkipsTheWeekdaysNotSelected()
    {
        // Every 7 minutes from Thursday 15 January 10:00, Mondays only: the first occurrence on Monday 19th
        // is the first one of the AtDate grid at or after midnight.
        DateTimeEventConfig config = Interval(Local(2026, 1, 15, 10, 0), 0, 7, 0);
        config.OnDays = [DayOfWeek.Monday];

        DateTime anchorUtc = Utc(2026, 1, 15, 9, 0);
        DateTime mondayStartUtc = Utc(2026, 1, 18, 23, 0);
        long periods = (long)Math.Ceiling((mondayStartUtc - anchorUtc) / TimeSpan.FromMinutes(7));

        Assert.AreEqual(anchorUtc.AddMinutes(7 * periods), Next(config, Utc(2026, 1, 15, 12, 0)));
    }

    [TestMethod]
    public void Interval_StaysEvenlySpacedAcrossDaylightSavingChanges()
    {
        // Every 30 minutes through both changes: always 30 real minutes apart.
        foreach (DateTime startUtc in new[] { Utc(2026, 3, 28, 23, 0), Utc(2026, 10, 24, 23, 0) })
        {
            DateTimeEventConfig config = Interval(DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(startUtc, _rome), DateTimeKind.Unspecified), 0, 30, 0);

            DateTime currentUtc = startUtc;
            for (int i = 0; i < 12; i++)
            {
                DateTime? nextUtc = Next(config, currentUtc);
                Assert.AreEqual(currentUtc.AddMinutes(30), nextUtc, $"Occurrence {i + 1} after {startUtc:u}");
                currentUtc = nextUtc!.Value;
            }
        }
    }

    // ---------------- Validation ----------------

    [TestMethod]
    public void Validate_RejectsAZeroInterval()
    {
        Assert.IsNotNull(DateTimeEventScheduler.Validate(Interval(Local(2026, 1, 15, 10, 0), 0, 0, 0)));
    }

    [TestMethod]
    public void Validate_RejectsANegativeInterval()
    {
        Assert.IsNotNull(DateTimeEventScheduler.Validate(Interval(Local(2026, 1, 15, 10, 0), 1, -5, 0)));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("24:00:00")]
    [DataRow("7")]
    [DataRow("aa:bb:cc")]
    public void Validate_RejectsAnInvalidTimeOfDay(string time)
    {
        Assert.IsNotNull(DateTimeEventScheduler.Validate(Daily(time)));
    }

    [TestMethod]
    public void Validate_AcceptsValidConfigurations()
    {
        Assert.IsNull(DateTimeEventScheduler.Validate(new DateTimeEventConfig()));
        Assert.IsNull(DateTimeEventScheduler.Validate(Daily("00:00:00")));
        Assert.IsNull(DateTimeEventScheduler.Validate(Daily("23:59:59")));
        Assert.IsNull(DateTimeEventScheduler.Validate(Interval(Local(2026, 1, 15, 10, 0), 0, 0, 1)));
    }
}
