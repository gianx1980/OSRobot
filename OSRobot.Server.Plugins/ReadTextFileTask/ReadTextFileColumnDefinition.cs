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

namespace OSRobot.Server.Plugins.ReadTextFileTask;

public class ReadTextFileColumnDefinition
{
    public ReadTextFileColumnDefinition()
    {
        
    }

    public ReadTextFileColumnDefinition(string name, ReadTextFileColumnDataType dataType, string expectedFormat, string expectedCulture,
                                        bool treatNullStringAsNullValue, bool isIdentity, int? colPosition, int? colStartsFromCharPos, int? colEndsAtCharPos)
    {
        ColumnName = name;
        ColumnDataType = dataType;
        ColumnExpectedFormat = expectedFormat;
        ColumnExpectedCulture = expectedCulture;
        ColumnTreatNullStringAsNull = treatNullStringAsNullValue;

        ColumnIsIdentity = isIdentity;
        ColumnPosition = colPosition;
        ColumnStartsFromCharPos = colStartsFromCharPos;
        ColumnEndsAtCharPos = colEndsAtCharPos;
    }

    public string ColumnName { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ReadTextFileColumnDataType ColumnDataType { get; set; }
    public string ColumnExpectedFormat { get; set; } = string.Empty;
    public string ColumnExpectedCulture { get; set; } = string.Empty;
    public bool ColumnTreatNullStringAsNull { get; set; }

    public bool ColumnIsIdentity { get; set; }
    public int? ColumnPosition { get; set; }
    public int? ColumnStartsFromCharPos { get; set; }
    public int? ColumnEndsAtCharPos { get; set; }

    public int ColumnWidth 
    { 
        get
        {
            if (ColumnStartsFromCharPos == null || ColumnEndsAtCharPos == null)
                return 0;

            return (int)(ColumnEndsAtCharPos - ColumnStartsFromCharPos + 1);
        }
    }

    public override string ToString()
    {
        return ColumnName;
    }
}
