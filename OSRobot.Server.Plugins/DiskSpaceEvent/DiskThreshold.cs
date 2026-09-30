// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json.Serialization;

namespace OSRobot.Server.Plugins.DiskSpaceEvent;

public enum DiskThresholdUnitMeasure
{
    Megabytes,
    Gigabytes,
    Terabytes,
    Percentage
}

public enum CheckOperator
{
    GreaterThan,
    LessThan
}

public class DiskThreshold
{
    public string Disk { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CheckOperator CheckOperator { get; set; }

    public int ThresholdValue { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DiskThresholdUnitMeasure UnitMeasure { get; set;}

    public override string ToString()
    {
        string result = Disk;

        if (CheckOperator == CheckOperator.GreaterThan)
            result += " " + Resource.TxtGreaterThan;
        else
            result += " " + Resource.TxtLessThan;

        result += " " + ThresholdValue;

        if (UnitMeasure == DiskThresholdUnitMeasure.Megabytes)
            result += " " + Resource.TxtMegabytes;
        else if (UnitMeasure == DiskThresholdUnitMeasure.Gigabytes)
            result += " " + Resource.TxtGigabytes;
        else if (UnitMeasure == DiskThresholdUnitMeasure.Terabytes)
            result += " " + Resource.TxtTerabytes;
        else if (UnitMeasure == DiskThresholdUnitMeasure.Percentage)
            result += " " + Resource.TxtPercentage;

        return result;
    }
}
