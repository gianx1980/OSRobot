// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using System.Text.Json.Serialization;

namespace OSRobot.Server.Plugins.ExcelFileTask;

public enum ExcelFileTaskType
{
    AppendRow,
    InsertRow,
    ReadRow,
    DeleteRow,
    FindFirstRow,
    FindAllRows,
    FindAndReplace
}

public enum ExcelReadIntervalType
{
    ReadFromRowToRow,
    ReadFromRowToLastRow,
    ReadLastNRows
}

public class ExcelFileTaskConfig : ITaskConfig
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public bool Log { get; set; } = true;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public IterationMode PluginIterationMode { get; set; }
    public string IterationObject { get; set; } = string.Empty;
    public int IterationsCount { get; set; }


    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ExcelFileTaskType TaskType { get; set; }


    // Insert / Append configuration
    [DynamicData]
    public string FilePath { get; set; } = string.Empty;
    [DynamicData]
    public string SheetName { get; set; } = string.Empty;

    public List<ExcelFileColumnDefinition> ColumnsDefinition { get; set; } = [];

    public bool AddHeaderIfEmpty { get; set; }
    [DynamicData]
    public string InsertAtRow { get; set; } = string.Empty;

    // Read configuration
    public bool ReadLastRowOption { get; set; } = true;
    public bool ReadRowNumberOption { get; set; }
    [DynamicData]
    public string ReadRowNumber { get; set; } = string.Empty;
    public bool ReadIntervalOption { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ExcelReadIntervalType ReadInterval { get; set; }
    [DynamicData]
    public string ReadFromRow { get; set; } = string.Empty;
    [DynamicData]
    public string ReadToRow { get; set; } = string.Empty;
    [DynamicData]
    public string ReadNumberOfRows { get; set; } = string.Empty;
    [DynamicData]
    public string NumColumnsToRead { get; set; } = string.Empty;

    // Delete configuration
    [DynamicData]
    public string DeleteRowNumber { get; set; } = string.Empty;

    // Find / Replace configuration
    [DynamicData]
    public string FindText { get; set; } = string.Empty;
    [DynamicData]
    public string ReplaceWith { get; set; } = string.Empty;
}
