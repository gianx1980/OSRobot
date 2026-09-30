// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Persistence;
using System.Text.Json.Serialization;

namespace OSRobot.Server.Plugins.SqlServerCommandTask;

public enum QueryTaskType
{
    Text,
    StoredProcedure
}

public class SqlServerCommandTaskConfig : ITaskConfig
{
    public const int _defaultCommandTimeout = 30;

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public bool Log { get; set; } = true;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public IterationMode PluginIterationMode { get; set; }
    public string IterationObject { get; set; } = string.Empty;
    public int IterationsCount { get; set; }

    [DynamicData]
    public string Query { get; set; } = string.Empty;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public QueryTaskType Type { get; set; }
    [DynamicData]
    public List<SqlServerParamDefinition> ParamsDefinition { get; set; } = [];

    public bool ReturnsRecordset { get; set; }

    [XmlEncryptField]
    public string Server { get; set; } = string.Empty;
    [XmlEncryptField]
    public string Database { get; set; } = string.Empty;
    [XmlEncryptField]
    public string Username { get; set; } = string.Empty;
    [XmlEncryptField]
    public string Password { get; set; } = string.Empty;
    [XmlEncryptField]
    public string ConnectionStringOptions { get; set; } = string.Empty;
    public int CommandTimeout { get; set; } = _defaultCommandTimeout;
}
