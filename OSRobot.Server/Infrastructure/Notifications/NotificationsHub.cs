// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OSRobot.Server.Infrastructure.Security;

namespace OSRobot.Server.Infrastructure.Notifications;

/// <summary>
/// Pushes NotifyUsersTask notifications to every open editor. Server-to-client only: it declares no
/// methods a client can call. See HubUserNotifier.
/// </summary>
[Authorize]
public class NotificationsHub : Hub
{
    public const string Path = "/hubs/notifications";
    public const string ClientMethod = "notification";

    public override Task OnConnectedAsync()
    {
        // MustChangePasswordFilter is an MVC filter and never runs for hubs: refuse here the users it
        // would block everywhere else, or they could still receive job data through notifications.
        if (Context.User?.FindFirst(OSRobotClaimTypes.MustChangePassword)?.Value == "true")
            Context.Abort();

        return base.OnConnectedAsync();
    }
}
