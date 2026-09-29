// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using System.Text.Json.Serialization;

namespace OSRobot.Server.Plugins.ReadTextFileTask;

public enum ReadTextFileIntervalType
{
    ReadFromRowToRow,
    ReadFromRowToLastRow,
    ReadLastNRows
}

public enum ReadTextFileSplitColumnsType
{
    None,
    UseDelimiters,
    UseFixedWidthColumns
}

public enum ReadTextFileColumnDataType
{
    String,
    Integer,
    Decimal,
    Datetime
}

public class ReadTextFileTaskConfig : ITaskConfig
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public bool Log { get; set; } = true;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public IterationMode PluginIterationMode { get; set; }
    public string IterationObject { get; set; } = string.Empty;
    public int IterationsCount { get; set; }

    [DynamicData]
    public string FilePath { get; set; } = string.Empty;

    public bool ReadAllTheRowsOption { get; set; } = true;
    public bool SkipFirstLine { get; set; }

    public bool ReadLastRowOption { get; set; }
    public bool ReadRowNumberOption { get; set; }
    [DynamicData]
    public string ReadRowNumber { get; set; } = string.Empty;
    public bool ReadIntervalOption { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ReadTextFileIntervalType ReadInterval { get; set; }
    [DynamicData]
    public string ReadFromRow { get; set; } = string.Empty;
    [DynamicData]
    public string ReadToRow { get; set; } = string.Empty;
    [DynamicData]
    public string ReadNumberOfRows { get; set; } = string.Empty;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ReadTextFileSplitColumnsType SplitColumnsType { get; set; } = ReadTextFileSplitColumnsType.None;

    public bool DelimiterTab { get; set; }

    public bool DelimiterSemicolon { get; set; }

    public bool DelimiterComma { get; set; }

    public bool DelimiterSpace { get; set; }

    public bool DelimiterOther { get; set; }

    public string DelimiterOtherChar { get; set; } = string.Empty;

    public bool UseDoubleQuotes { get; set; }

    public List<ReadTextFileColumnDefinition> ColumnsDefinition { get; set; } = [];
}
