// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.Logging;
using OSRobot.Server.Core.Notifications;
using OSRobot.Tests.Support;

namespace OSRobot.Tests.TestClasses;

// Core.UserNotifier is process-wide.
[TestClass]
[DoNotParallelize]
public sealed class TestUserNotifierWiring
{
    [TestCleanup]
    public void RestoreNoOpNotifier() => Core.Init(PluginInstanceLogger.LogPath);

    [TestMethod]
    public void Init_without_notifier_installs_the_no_op()
    {
        Core.Init(PluginInstanceLogger.LogPath, userNotifier: new RecordingUserNotifier());

        Core.Init(PluginInstanceLogger.LogPath);

        Assert.AreSame(NullUserNotifier.Instance, Core.UserNotifier);
    }

    [TestMethod]
    public void Init_installs_the_given_notifier()
    {
        RecordingUserNotifier notifier = new();

        Core.Init(PluginInstanceLogger.LogPath, userNotifier: notifier);

        Assert.AreSame(notifier, Core.UserNotifier);
    }

    [TestMethod]
    public void Engine_start_installs_its_notifier()
    {
        RecordingUserNotifier notifier = new();

        using EngineHarness harness = new(new JobGraph(), userNotifier: notifier);

        Assert.AreSame(notifier, Core.UserNotifier);
    }
}
