// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Collections.Concurrent;
using OSRobot.Server.Core.Notifications;

namespace OSRobot.Tests.Support;

/// <summary>Records every notification it is asked to send; can be told to fail instead.</summary>
public sealed class RecordingUserNotifier : IUserNotifier
{
    private readonly ConcurrentQueue<UserNotification> _notifications = new();

    public IReadOnlyList<UserNotification> Notifications => [.. _notifications];

    /// <summary>When set and it returns an exception, NotifyAsync throws it instead of recording.</summary>
    public Func<UserNotification, Exception?>? FailWith { get; set; }

    public Task NotifyAsync(UserNotification notification, CancellationToken cancellationToken)
    {
        Exception? failure = FailWith?.Invoke(notification);
        if (failure != null)
            throw failure;

        _notifications.Enqueue(notification);
        return Task.CompletedTask;
    }
}
