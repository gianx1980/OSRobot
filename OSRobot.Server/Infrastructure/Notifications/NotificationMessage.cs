// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core.Notifications;

namespace OSRobot.Server.Infrastructure.Notifications;

/// <summary>What the editor receives (serialized camelCase: title, message, level, timestamp, sourceName).</summary>
public sealed record NotificationMessage(string Title, string Message, string Level, DateTime Timestamp, string SourceName)
{
    public static NotificationMessage From(UserNotification notification)
    {
        return new NotificationMessage(notification.Title, notification.Message, notification.Level.ToString(),
                                       notification.Timestamp, notification.SourceName);
    }
}
