// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using System.Text.Json.Serialization;

namespace OSRobot.Server.Plugins.FtpSftpTask;

public enum ProtocolEnum
{
    FTP,
    SFTP
}

public enum CommandEnum
{
    Copy,
    Delete
}


public class FtpSftpTaskConfig : ITaskConfig
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
    public ProtocolEnum Protocol { get; set; }

    [DynamicData]
    public string Host { get; set; } = string.Empty;
    [DynamicData]
    public string Port { get; set; } = string.Empty;
    [DynamicData]
    public string Username { get; set; } = string.Empty;
    [DynamicData]
    public string Password { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CommandEnum Command { get; set; }


    public List<FtpSftpCopyItem> CopyItems { get; } = [];

    public List<FtpSftpDeleteItem> DeleteItems { get; } = [];
}
