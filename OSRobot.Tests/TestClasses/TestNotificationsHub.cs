// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Security.Claims;
using OSRobot.Server.Core.Notifications;
using OSRobot.Server.Infrastructure.Notifications;
using OSRobot.Server.Infrastructure.Security;
using OSRobot.Tests.Fakes;

namespace OSRobot.Tests.TestClasses;

[TestClass]
public sealed class TestNotificationsHub
{
    private static ClaimsPrincipal AuthenticatedUser(params Claim[] claims) =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "admin"), .. claims], authenticationType: "Test"));

    [TestMethod]
    public async Task Notifier_broadcasts_the_notification_to_all_clients()
    {
        FakeHubContext<NotificationsHub> hubContext = new();
        HubUserNotifier notifier = new(hubContext);
        DateTime timestamp = new(2026, 10, 20, 9, 30, 0);

        await notifier.NotifyAsync(new UserNotification("T", "M", UserNotificationLevel.Error, timestamp, "Src"), CancellationToken.None);

        (string method, object?[] args) = hubContext.AllClients.Sent.Single();
        Assert.AreEqual("notification", method);
        Assert.AreEqual(new NotificationMessage("T", "M", "Error", timestamp, "Src"), args.Single());
    }

    [TestMethod]
    public async Task OnConnected_aborts_when_password_change_is_required()
    {
        FakeHubCallerContext connection = new(AuthenticatedUser(new Claim(OSRobotClaimTypes.MustChangePassword, "true")));
        NotificationsHub hub = new() { Context = connection };

        await hub.OnConnectedAsync();

        Assert.IsTrue(connection.Aborted);
    }

    [TestMethod]
    public async Task OnConnected_accepts_a_normal_user()
    {
        FakeHubCallerContext connection = new(AuthenticatedUser());
        NotificationsHub hub = new() { Context = connection };

        await hub.OnConnectedAsync();

        Assert.IsFalse(connection.Aborted);
    }
}
