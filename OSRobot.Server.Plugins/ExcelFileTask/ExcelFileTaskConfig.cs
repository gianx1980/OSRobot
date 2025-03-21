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
