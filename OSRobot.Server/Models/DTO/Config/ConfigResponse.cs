// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Models.DTO.Config;

public class ConfigResponse(int requestNewTokenIfMinutesLeft, string appTitle, string staticFilesUrl, int heartBeatInterval,
                        bool notificationServerSentEventsEnabled, int notificationPollingInterval, string serverVersion)
{
    public int RequestNewTokenIfMinutesLeft { get; set; } = requestNewTokenIfMinutesLeft;
    public string AppTitle { get; set; } = appTitle;
    public string StaticFilesUrl { get; set; } = staticFilesUrl;
    public int HeartBeatInterval { get; set; } = heartBeatInterval;
    public bool NotificationServerSentEventsEnabled { get; set; } = notificationServerSentEventsEnabled;
    public int NotificationPollingInterval { get; set; } = notificationPollingInterval;
    public string ServerVersion { get; set; } = serverVersion;
}
