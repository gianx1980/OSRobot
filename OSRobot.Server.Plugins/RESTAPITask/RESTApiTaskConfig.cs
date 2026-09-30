// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using System.Text.Json.Serialization;

namespace OSRobot.Server.Plugins.RESTApiTask;

public enum MethodType
{
    Get,
    Post,
    Put,
    Delete
}

public class RESTApiTaskConfig : ITaskConfig
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
    public string URL { get; set; } = string.Empty;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public MethodType Method { get; set; }
    public List<RESTApiHeader> Headers { get; set; } = [];

    [DynamicData]
    public string Body { get; set; } = string.Empty;
    [DynamicData]
    public string JsonPathToData { get; set; } = string.Empty;
    public bool ReturnsRecordset { get; set; }
}
