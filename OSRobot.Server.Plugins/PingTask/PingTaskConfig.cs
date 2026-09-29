// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using System.Text.Json.Serialization;

namespace OSRobot.Server.Plugins.PingTask;

public class PingTaskConfig : ITaskConfig
{
    private const int _defaultTimeout = 1000;
    private const int _defaultAttempts = 4;
    
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public bool Log { get; set; } = true;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public IterationMode PluginIterationMode { get; set; }

    public string IterationObject { get; set; } = string.Empty;
    public int IterationsCount { get; set; }

    [DynamicData]
    public string Host { get; set; } = string.Empty;
    public int Timeout { get; set; } = _defaultTimeout;
    public int Attempts { get; set; } = _defaultAttempts;
}
