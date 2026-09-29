// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;

namespace OSRobot.Server.Plugins.DateTimeEvent;

public class DateTimeEventConfig : IEventConfig
{
    private const int _defaultEveryNumMinutes = 5;
    private const int _defaultEveryNumSeconds = 5;

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public bool Log { get; set; } = true;
    public DateTime AtDate { get; set; } = DateTime.Now;
    public bool OneTime { get; set; } = true;
    
    public bool EveryDaysHoursSecs { get; set; }
    public int EveryNumDays { get; set; }
    public int EveryNumHours { get; set; }
    public int EveryNumMinutes { get; set; } = _defaultEveryNumMinutes;

    public bool EverySeconds { get; set; }
    public int EveryNumSeconds { get; set; } = _defaultEveryNumSeconds;

    public List<DayOfWeek> OnDays { get; set; } = [ DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                                                                            DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday ];
    public bool OnAllDays { get; set; } = true;
}
