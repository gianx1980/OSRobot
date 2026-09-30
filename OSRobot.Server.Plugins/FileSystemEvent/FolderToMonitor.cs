// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json.Serialization;

namespace OSRobot.Server.Plugins.FileSystemEvent;

public enum MonitorActionType
{
    NewFiles,
    ModifiedFiles,
    DeletedFiles
}

public class FolderToMonitor
{
    public string Path { get; set; } = string.Empty;
    public bool MonitorSubFolders { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public MonitorActionType MonitorAction { get; set; }

    public override string ToString()
    {
        return Path;
    }
}
