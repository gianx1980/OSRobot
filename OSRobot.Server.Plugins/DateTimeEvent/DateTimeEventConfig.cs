// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using System.Text.Json.Serialization;

namespace OSRobot.Server.Plugins.DateTimeEvent;

public enum DateTimeEventSchedule
{
    /// <summary>Once, at AtDate.</summary>
    OneTime,

    /// <summary>Every day at DailyTime, on the selected weekdays.</summary>
    EveryDayAt,

    /// <summary>Every IntervalHours/IntervalMinutes/IntervalSeconds, aligned to AtDate, on the selected weekdays.</summary>
    Interval
}

public class DateTimeEventConfig : IEventConfig
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public bool Log { get; set; } = true;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DateTimeEventSchedule Schedule { get; set; } = DateTimeEventSchedule.OneTime;

    /// <summary>
    /// Local wall-clock time. OneTime: when the event fires. Interval: the first occurrence, which every
    /// following one is aligned to (AtDate + N * interval). Defaults to the current minute.
    /// </summary>
    public DateTime AtDate { get; set; } = DateTime.Now.Date.AddMinutes(Math.Floor(DateTime.Now.TimeOfDay.TotalMinutes));

    /// <summary>EveryDayAt: the local time of day, as "HH:mm:ss".</summary>
    public string DailyTime { get; set; } = "00:00:00";

    public int IntervalHours { get; set; }
    public int IntervalMinutes { get; set; } = 5;
    public int IntervalSeconds { get; set; }

    /// <summary>EveryDayAt and Interval: the weekdays the event fires on. OneTime ignores it.</summary>
    public List<DayOfWeek> OnDays { get; set; } = [ DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                                                    DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday ];
}
