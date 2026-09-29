// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Models.DTO.Config;

public class ConfigResponse(int requestNewTokenIfMinutesLeft, string appTitle, string staticFilesUrl, int heartBeatInterval,
                        bool notificationServerSentEventsEnabled, int notificationPollingInterval)
{
    public int RequestNewTokenIfMinutesLeft { get; set; } = requestNewTokenIfMinutesLeft;
    public string AppTitle { get; set; } = appTitle;
    public string StaticFilesUrl { get; set; } = staticFilesUrl;
    public int HeartBeatInterval { get; set; } = heartBeatInterval;
    public bool NotificationServerSentEventsEnabled { get; set; } = notificationServerSentEventsEnabled;
    public int NotificationPollingInterval { get; set; } = notificationPollingInterval;
}
