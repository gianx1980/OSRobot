// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using System.Text.Json.Serialization;

namespace OSRobot.Server.Plugins.ZipTask;

public enum IfArchiveExistsType
{
    AddOrOverwriteFilesInTheArchive,
    CreateWithUniqueNames,
    Fail
}

public enum CompressionLevelType
{
    Low,
    Medium,
    High
}

public class ZipTaskConfig : ITaskConfig
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public bool Log { get; set; } = true;

    [DynamicData]
    public string Source { get; set; } = string.Empty;
    [DynamicData]
    public string Destination { get; set; } = string.Empty;
    public bool IncludeFilesInSubFolders { get; set; }
    public bool StoreFullPath { get; set; }
    public bool SkipEmptyFolders { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public IfArchiveExistsType IfArchiveExists { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CompressionLevelType CompressionLevel { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public IterationMode PluginIterationMode { get; set; }
    public string IterationObject { get; set; } = string.Empty;
    public int IterationsCount { get; set; }
}
