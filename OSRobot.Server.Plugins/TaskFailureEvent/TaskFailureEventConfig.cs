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
    /// Skip failures the job already reacts to: those for which one of the task's connections started
    /// something (e.g. an "Object does not execute" connection).
    /// </summary>
    public bool ReportOnlyUnhandled { get; set; } = true;

    /// <summary>
    /// 0: the event fires once per failure. Otherwise the first failure opens a window of this many
    /// seconds, and the event fires once at its end with every failure of the window.
    /// </summary>
    public int GroupForSeconds { get; set; }
}
