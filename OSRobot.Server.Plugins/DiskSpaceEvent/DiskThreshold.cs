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
