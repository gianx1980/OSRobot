// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using System.Text.Json.Serialization;

namespace OSRobot.Server.Plugins.RunProgramTask;

public class RunProgramTaskConfig : ITaskConfig
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
    public string ProgramPath { get; set; } = string.Empty;
    [DynamicData]
    public string Parameters { get; set; } = string.Empty;
    [DynamicData]
    public string WorkingFolder { get; set; } = string.Empty;
}
