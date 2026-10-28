// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json.Serialization;
using OSRobot.Server.Core;

namespace OSRobot.Server.Plugins.TaskFailureEvent;

/// <summary>Which tasks' failures the event reports, relative to the folder the event is in.</summary>
public enum TaskFailureEventScope
{
    ThisFolderAndSubfolders,
    ThisFolder,
    AllFolders
}

public class TaskFailureEventConfig : IEventConfig
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public bool Log { get; set; } = true;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TaskFailureEventScope Scope { get; set; } = TaskFailureEventScope.ThisFolderAndSubfolders;

    /// <summary>
    /// 0: the event fires once per failure. Otherwise the first failure opens a window of this many
    /// seconds, and the event fires once at its end with every failure of the window.
    /// </summary>
    public int GroupForSeconds { get; set; }
}
