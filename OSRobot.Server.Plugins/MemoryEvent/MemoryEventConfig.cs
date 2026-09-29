// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

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


