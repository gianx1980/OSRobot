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

namespace OSRobot.Server.Plugins.MemoryEvent;

public class MemoryEventConfig : IEventConfig
{
    private const int _defaultCheckIntervalSeconds = 1;
    private const float _defaultThreshold = 70;
    private const int _defaultAvgIntervalMinutes = 3;
    private const int _defaultMinutesFromLastTrigger = 5;

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public bool Log { get; set; } = true;

    public float? Threshold { get; set; } = _defaultThreshold;
    public bool TriggerIfUsageIsAboveThreshold { get; set; } = true;

    public float? ThresholdLastXMin { get; set; } = _defaultThreshold;
    public bool TriggerIfAvgUsageIsAboveThresholdLastXMin { get; set; }

    public int? AvgIntervalMinutes { get; set; } = _defaultAvgIntervalMinutes;

    public bool TriggerIfPassedXMinFromLastTrigger { get; set; }

    public int? MinutesFromLastTrigger { get; set; } = _defaultMinutesFromLastTrigger;

    public int CheckIntervalSeconds { get; set; } = _defaultCheckIntervalSeconds;
}


