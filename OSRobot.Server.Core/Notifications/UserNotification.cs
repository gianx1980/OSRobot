// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Core.Notifications;

public enum UserNotificationLevel
{
    Info,
    Success,
    Warning,
    Error
}

/// <param name="SourceName">Name of the task that sent it.</param>
public sealed record UserNotification(string Title, string Message, UserNotificationLevel Level, DateTime Timestamp, string SourceName);
