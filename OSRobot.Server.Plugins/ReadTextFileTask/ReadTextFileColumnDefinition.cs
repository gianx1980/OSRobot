// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

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
