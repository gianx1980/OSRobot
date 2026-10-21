// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Notifications;
using System.Text.Json.Serialization;

namespace OSRobot.Server.Plugins.NotifyUsersTask;

public class NotifyUsersTaskConfig : ITaskConfig
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public bool Log { get; set; } = true;

    [DynamicData]
    public string Title { get; set; } = string.Empty;
    [DynamicData]
    public string Message { get; set; } = string.Empty;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UserNotificationLevel Level { get; set; } = UserNotificationLevel.Info;

    // Like other tasks: one notification per row of the previous task's default recordset, or exactly
    // one when the previous object has none (an event, a task without rows).
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public IterationMode PluginIterationMode { get; set; } = IterationMode.IterateDefaultRecordset;
    public string IterationObject { get; set; } = string.Empty;
    public int IterationsCount { get; set; } = 1;
}
