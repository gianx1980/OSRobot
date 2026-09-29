// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Persistence;
using System.Text.Json.Serialization;

namespace OSRobot.Server.Plugins.SqlServerBulkCopyTask;

public class SqlServerBulkCopyTaskConfig : ITaskConfig
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

    // Don't need to mark as [DynamicData]
    public string SourceRecordset { get; set; } = string.Empty; 

    [DynamicData]
    public string DestinationTable { get; set; } = string.Empty;

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
