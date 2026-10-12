// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Persistence;
using System.Text.Json.Serialization;

namespace OSRobot.Server.Plugins.SqlServerFileLoaderTask;

public enum IfTableExistsAction
{
    /// <summary>Fail the task, leaving the table untouched.</summary>
    Fail,
    /// <summary>Load the rows into the existing table, matching the file's columns by name.</summary>
    Append,
    /// <summary>Drop the table and create it again from the file.</summary>
    DropAndRecreate
}

public class SqlServerFileLoaderTaskConfig : ITaskConfig
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

    /// <summary>A delimited text file (separator detected automatically) or an Excel .xlsx file.</summary>
    [DynamicData]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>Text files only: when false, <see cref="Separator"/> is used instead of detecting it.</summary>
    public bool AutodetectSeparator { get; set; } = true;

    /// <summary>Text files only, when not autodetected: the column separator. "\t" stands for a tab.</summary>
    public string Separator { get; set; } = string.Empty;

    /// <summary>Text files only: when false, <see cref="Encoding"/> is used instead of detecting it.</summary>
    public bool AutodetectEncoding { get; set; } = true;

    /// <summary>
    /// Text files only, when not autodetected: the file's encoding name, e.g. "utf-8", "windows-1252".
    /// A byte order mark at the start of the file still wins, since it states the encoding for certain.
    /// </summary>
    public string Encoding { get; set; } = string.Empty;

    /// <summary>Excel only: the sheet to load. Empty: the first sheet.</summary>
    [DynamicData]
    public string SheetName { get; set; } = string.Empty;

    /// <summary>Destination table, optionally with its schema: "MyTable", "dbo.MyTable", "[My schema].[My table]".</summary>
    [DynamicData]
    public string TableName { get; set; } = string.Empty;

    public bool FirstRowContainsColumnNames { get; set; } = true;

    /// <summary>When false, every column is created as text.</summary>
    public bool AutodetectColumnTypes { get; set; } = true;

    /// <summary>How numbers and dates are written in the file, e.g. "it-IT", "en-US". Empty: the server's regional settings.</summary>
    public string Culture { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public IfTableExistsAction IfTableExists { get; set; } = IfTableExistsAction.Fail;

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
