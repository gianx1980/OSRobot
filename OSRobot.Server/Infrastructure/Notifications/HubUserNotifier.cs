// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.AspNetCore.SignalR;
using OSRobot.Server.Core.Notifications;

namespace OSRobot.Server.Infrastructure.Notifications;

/// <summary>The server's Core.UserNotifier: broadcasts to every editor connected to NotificationsHub.</summary>
public sealed class HubUserNotifier(IHubContext<NotificationsHub> hubContext) : IUserNotifier
{
    private readonly IHubContext<NotificationsHub> _hubContext = hubContext;

    public Task NotifyAsync(UserNotification notification, CancellationToken cancellationToken)
    {
        return _hubContext.Clients.All.SendAsync(NotificationsHub.ClientMethod, NotificationMessage.From(notification), cancellationToken);
    }
}
