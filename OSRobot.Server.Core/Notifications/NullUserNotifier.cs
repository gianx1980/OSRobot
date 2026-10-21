// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Core.Notifications;

/// <summary>Used when the host provides no notifier (tests, other hosts): notifications go nowhere.</summary>
public sealed class NullUserNotifier : IUserNotifier
{
    public static NullUserNotifier Instance { get; } = new();

    private NullUserNotifier()
    {
    }

    public Task NotifyAsync(UserNotification notification, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
