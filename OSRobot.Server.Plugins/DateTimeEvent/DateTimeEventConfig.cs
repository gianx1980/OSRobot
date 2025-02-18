/*======================================================================================
    Copyright 2025 by Gianluca Di Bucci (gianx1980) (https://www.os-robot.com)

    This file is part of OSRobot.

    OSRobot is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    OSRobot is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with OSRobot.  If not, see <http://www.gnu.org/licenses/>.
======================================================================================*/
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

    public List<DayOfWeek> OnDays { get; set; } = new List<DayOfWeek>() { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                                                                            DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };
    public bool OnAllDays { get; set; } = true;
}
