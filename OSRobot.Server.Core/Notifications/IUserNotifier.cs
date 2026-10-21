// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Core.Notifications;

/// <summary>
/// Sends a one-way notification to the users currently listening (in the server: every open editor).
/// Fire-and-forget: nothing is stored, and nobody listening is not an error. An exception means the
/// notification could not be sent at all.
/// </summary>
public interface IUserNotifier
{
    Task NotifyAsync(UserNotification notification, CancellationToken cancellationToken);
}
