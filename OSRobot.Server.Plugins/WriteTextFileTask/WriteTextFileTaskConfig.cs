// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using System.Globalization;
using System.Text.Json.Serialization;

namespace OSRobot.Server.Plugins.WriteTextFileTask;

public enum WriteTextFileTaskType
{
    AppendRow,
    InsertRow,
    ReplaceText
}

public class WriteTextFileTaskConfig : ITaskConfig
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
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public WriteTextFileTaskType TaskType { get; set; }

    public List<WriteTextFileColumnDefinition> ColumnsDefinition { get; set; } = [];

    public bool FormatAsDelimitedFile { get; set; }

    public bool DelimiterTab { get; set; }

    public bool DelimiterSemicolon { get; set; }

    public bool DelimiterComma { get; set; }

    public bool DelimiterSpace { get; set; }

    public bool DelimiterOther { get; set; }

    public string DelimiterOtherChar { get; set; } = string.Empty;

    public bool EncloseInDoubleQuotes { get; set; }

    public bool FormatAsFixedLengthColumnsFile { get; set; }

    public bool AddHeaderIfEmpty { get; set; }

    [DynamicData]
    public string InsertAtRow { get; set; } = string.Empty;

    [DynamicData]
    public string FindText { get; set; } = string.Empty;

    [DynamicData]
    public string ReplaceWithText { get; set; } = string.Empty;
}
