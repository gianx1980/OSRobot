// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Globalization;

namespace OSRobot.Server.Plugins.DateTimeEvent;

/// <summary>
/// Computes a DateTimeEvent's occurrences. Pure (no timers, no clock reads), so the schedule can be
/// tested for any moment and time zone.
///
/// Occurrences are returned as UTC instants, so the time until the next one is real elapsed time
/// even across a daylight saving change. The configuration is local wall-clock time:
/// - OneTime and EveryDayAt are wall-clock times: "every day at 02:00" stays at 02:00 after a change.
/// - Interval is elapsed time: "every 30 minutes" stays 30 real minutes apart through a change.
/// </summary>
public static class DateTimeEventScheduler
{
    private static readonly string[] _dailyTimeFormats = [@"hh\:mm\:ss", @"hh\:mm"];

    /// <returns>A description of what is wrong with the configuration, or null if it can be scheduled.</returns>
    public static string? Validate(DateTimeEventConfig config)
    {
        switch (config.Schedule)
        {
            case DateTimeEventSchedule.EveryDayAt:
                if (!TryParseDailyTime(config.DailyTime, out _))
                    return $"Invalid time of day '{config.DailyTime}': expected HH:mm:ss.";
                break;

            case DateTimeEventSchedule.Interval:
                if (config.IntervalHours < 0 || config.IntervalMinutes < 0 || config.IntervalSeconds < 0)
                    return "The interval cannot be negative.";
                if (GetInterval(config) <= TimeSpan.Zero)
                    return "The interval must be greater than zero.";
                break;
        }

        return null;
    }

    /// <summary>The first occurrence strictly after <paramref name="afterUtc"/>, or null if there is none.</summary>
    /// <remarks>Expects a configuration that passes <see cref="Validate"/>.</remarks>
    public static DateTime? GetNextOccurrence(DateTimeEventConfig config, DateTime afterUtc, TimeZoneInfo timeZone)
    {
        return config.Schedule switch
        {
            DateTimeEventSchedule.OneTime => GetNextOneTime(config, afterUtc, timeZone),
            DateTimeEventSchedule.EveryDayAt => GetNextEveryDayAt(config, afterUtc, timeZone),
            DateTimeEventSchedule.Interval => GetNextInterval(config, afterUtc, timeZone),
            _ => throw new ArgumentOutOfRangeException(nameof(config), config.Schedule, "Unknown schedule.")
        };
    }

    /// <summary>
    /// Converts a local wall-clock time of <paramref name="timeZone"/> to UTC.
    /// </summary>
    public static DateTime ToUtc(DateTime wallClock, TimeZoneInfo timeZone)
    {
        DateTime time = DateTime.SpecifyKind(wallClock, DateTimeKind.Unspecified);

        if (timeZone.IsInvalidTime(time))
        {
            // Skipped by a daylight saving change (clocks moved forward): the occurrence happens when the
            // clock jumps, at the first valid minute, rather than not at all.
            time = new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, 0, DateTimeKind.Unspecified);
            while (timeZone.IsInvalidTime(time))
                time = time.AddMinutes(1);
        }

        // An ambiguous time (repeated when clocks move back) is taken as standard time, i.e. its second
        // occurrence, so it happens once.
        return TimeZoneInfo.ConvertTimeToUtc(time, timeZone);
    }

    public static bool TryParseDailyTime(string? value, out TimeSpan timeOfDay)
    {
        return TimeSpan.TryParseExact(value, _dailyTimeFormats, CultureInfo.InvariantCulture, out timeOfDay)
               && timeOfDay >= TimeSpan.Zero && timeOfDay < TimeSpan.FromDays(1);
    }

    private static TimeSpan GetInterval(DateTimeEventConfig config)
    {
        return new TimeSpan(config.IntervalHours, config.IntervalMinutes, config.IntervalSeconds);
    }

    private static DateTime? GetNextOneTime(DateTimeEventConfig config, DateTime afterUtc, TimeZoneInfo timeZone)
    {
        DateTime atUtc = ToUtc(config.AtDate, timeZone);
        return atUtc > afterUtc ? atUtc : null;
    }

    private static DateTime? GetNextEveryDayAt(DateTimeEventConfig config, DateTime afterUtc, TimeZoneInfo timeZone)
    {
        if (!TryParseDailyTime(config.DailyTime, out TimeSpan timeOfDay))
            throw new FormatException($"Invalid time of day '{config.DailyTime}'.");

        DateTime afterLocal = TimeZoneInfo.ConvertTimeFromUtc(afterUtc, timeZone);

        // Today's occurrence may have passed already, and only one weekday may be selected: within 8 days
        // there is always one, if any weekday is selected at all.
        for (int day = 0; day <= 7; day++)
        {
            DateTime date = afterLocal.Date.AddDays(day);
            if (!config.OnDays.Contains(date.DayOfWeek))
                continue;

            DateTime occurrenceUtc = ToUtc(date + timeOfDay, timeZone);
            if (occurrenceUtc > afterUtc)
                return occurrenceUtc;
        }

        return null;
    }

    private static DateTime? GetNextInterval(DateTimeEventConfig config, DateTime afterUtc, TimeZoneInfo timeZone)
    {
        TimeSpan interval = GetInterval(config);
        if (interval <= TimeSpan.Zero)
            throw new ArgumentException("The interval must be greater than zero.", nameof(config));

        DateTime anchorUtc = ToUtc(config.AtDate, timeZone);

        DateTime candidateUtc = anchorUtc > afterUtc
            ? anchorUtc
            : anchorUtc.AddTicks(interval.Ticks * (((afterUtc - anchorUtc).Ticks / interval.Ticks) + 1));

        // Skip the weekdays that aren't selected by jumping to the first occurrence of the next day,
        // instead of stepping through every interval in between (one second over six days is a lot of steps).
        for (int attempt = 0; attempt <= 7; attempt++)
        {
            DateTime candidateLocal = TimeZoneInfo.ConvertTimeFromUtc(candidateUtc, timeZone);
            if (config.OnDays.Contains(candidateLocal.DayOfWeek))
                return candidateUtc;

            DateTime nextDayStartUtc = ToUtc(candidateLocal.Date.AddDays(1), timeZone);
            long periods = ((nextDayStartUtc - anchorUtc).Ticks + interval.Ticks - 1) / interval.Ticks;
            candidateUtc = anchorUtc.AddTicks(interval.Ticks * periods);
        }

        return null;
    }
}
