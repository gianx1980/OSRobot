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
using OSRobot.Server.Core.Persistence;
using System.Text.Json.Serialization;

namespace OSRobot.Server.Plugins.SqlServerBackupTask;

public enum DatabasesToBackupEnum
{
    AllDatabases,
    AllUserDatabases,
    SelectedDatabases
}

public enum UseCompressionEnum
{
    UseServerDefault,
    CompressBackup,
    DoNotCompressBackup
}

public enum BackupTypeEnum
{
    Full,
    TransactionLog
}

public class SqlServerBackupTaskConfig : ITaskConfig
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public bool Log { get; set; } = true;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public IterationMode PluginIterationMode { get; set; } 
    public string IterationObject { get; set; } = string.Empty;
    public int IterationsCount { get; set; }


    [XmlEncryptField]
    public string Server { get; set; } = string.Empty;
    [XmlEncryptField]
    public string Username { get; set; } = string.Empty;
    [XmlEncryptField]
    public string Password { get; set; } = string.Empty;
    [XmlEncryptField]
    public string ConnectionStringOptions { get; set; } = string.Empty;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public BackupTypeEnum BackupType { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DatabasesToBackupEnum DatabasesToBackup { get; set; }

    public List<string> DatabasesList { get; } = new List<string>();

    public List<string> SelectedDatabases { get; } = new List<string>();

    [DynamicData]
    public string DestinationPath { get; set; } = string.Empty;

    [DynamicData]
    public string FileNameTemplate { get; set; } = string.Empty;

    public bool OverwriteIfExists { get; set; }

    public bool VerifyBackup { get; set; }
    public bool PerformChecksum { get; set; }
    public bool ContinueOnError { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UseCompressionEnum UseCompression { get; set; }
}
