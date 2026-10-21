// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Data;
using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging;
using OSRobot.Server.Core.Logging.Abstract;
using OSRobot.Server.Core.Notifications;
using OSRobot.Server.Plugins.NotifyUsersTask;
using OSRobot.Tests.Support;

namespace OSRobot.Tests.TestPlugins;

// Core.UserNotifier is process-wide.
[TestClass]
[DoNotParallelize]
public sealed class TestNotifyUsersTask
{
    private RecordingUserNotifier _notifier = null!;

    [TestInitialize]
    public void InstallRecordingNotifier()
    {
        Common.ConfigureLogPath();
        _notifier = new RecordingUserNotifier();
        Core.Init(PluginInstanceLogger.LogPath, userNotifier: _notifier);
    }

    [TestCleanup]
    public void RestoreNoOpNotifier() => Core.Init(PluginInstanceLogger.LogPath);

    private static NotifyUsersTaskConfig Config(string title, string message = "", int iterations = 1) => new()
    {
        Id = 1,
        Name = "Notify 1",
        Title = title,
        Message = message,
        PluginIterationMode = IterationMode.IterateExactNumber,
        IterationsCount = iterations
    };

    private static async Task<List<ExecResult>> RunAsync(NotifyUsersTaskConfig config, CancellationToken cancellationToken = default,
                                                         DynamicDataSet? previousTaskData = null)
    {
        NotifyUsersTask task = new() { Config = config, ParentFolder = Common.CreateRootFolder() };
        (DynamicDataChain chain, DynamicDataSet set, IPluginInstanceLogger logger) = Common.GetTaskDefaultParameters(task);
        InstanceExecResult result = await task.RunAsync(chain, previousTaskData ?? set, null, logger, cancellationToken);
        return result.ExecResults;
    }

    private static NotifyUsersTaskConfig DefaultConfig(string title)
    {
        NotifyUsersTaskConfig config = (NotifyUsersTaskConfig)new NotifyUsersTaskPlugin().GetPluginDefaultConfig();
        config.Title = title;
        return config;
    }

    [TestMethod]
    public void Default_config_iterates_the_previous_tasks_default_recordset()
    {
        NotifyUsersTaskConfig config = (NotifyUsersTaskConfig)new NotifyUsersTaskPlugin().GetPluginDefaultConfig();

        Assert.AreEqual(IterationMode.IterateDefaultRecordset, config.PluginIterationMode);
    }

    [TestMethod]
    public async Task Default_config_sends_once_when_the_previous_task_has_no_default_recordset()
    {
        List<ExecResult> results = await RunAsync(DefaultConfig("After an event"));

        Assert.HasCount(1, _notifier.Notifications);
        Assert.IsTrue(results.Single().Result);
    }

    [TestMethod]
    public async Task Default_config_sends_once_per_row_of_the_previous_tasks_default_recordset()
    {
        DataTable rows = new();
        rows.Columns.Add("File", typeof(string));
        rows.Rows.Add("a.txt");
        rows.Rows.Add("b.txt");
        rows.Rows.Add("c.txt");
        DynamicDataSet previousTaskData = new() { [CommonDynamicData.DefaultRecordsetName] = rows };

        await RunAsync(DefaultConfig("File processed"), previousTaskData: previousTaskData);

        Assert.HasCount(3, _notifier.Notifications);
    }

    [TestMethod]
    public async Task Sends_title_message_and_level()
    {
        NotifyUsersTaskConfig config = Config("Backup", "Done");
        config.Level = UserNotificationLevel.Warning;

        List<ExecResult> results = await RunAsync(config);

        UserNotification notification = _notifier.Notifications.Single();
        Assert.AreEqual("Backup", notification.Title);
        Assert.AreEqual("Done", notification.Message);
        Assert.AreEqual(UserNotificationLevel.Warning, notification.Level);
        Assert.AreEqual("Notify 1", notification.SourceName);
        Assert.IsTrue(results.Single().Result);
    }

    [TestMethod]
    public async Task Resolves_dynamic_data_in_title_and_message()
    {
        Environment.SetEnvironmentVariable("OSR_NOTIFY_TEST", "srv01");
        try
        {
            await RunAsync(Config("Host {environment['OSR_NOTIFY_TEST']}", "On {environment['OSR_NOTIFY_TEST']} too"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("OSR_NOTIFY_TEST", null);
        }

        UserNotification notification = _notifier.Notifications.Single();
        Assert.AreEqual("Host srv01", notification.Title);
        Assert.AreEqual("On srv01 too", notification.Message);
    }

    [TestMethod]
    public async Task Sends_one_notification_per_iteration()
    {
        List<ExecResult> results = await RunAsync(Config("Tick", iterations: 3));

        Assert.HasCount(3, _notifier.Notifications);
        Assert.HasCount(3, results);
        Assert.IsTrue(results.All(r => r.Result));
    }

    [TestMethod]
    public async Task Blank_title_fails_the_iteration()
    {
        List<ExecResult> results = await RunAsync(Config("   ", "x", iterations: 2));

        Assert.HasCount(2, results);
        Assert.IsTrue(results.All(r => !r.Result));
        Assert.IsEmpty(_notifier.Notifications);
    }

    [TestMethod]
    public async Task Long_title_and_message_are_truncated()
    {
        await RunAsync(Config(new string('t', 250), new string('m', 2500)));

        UserNotification notification = _notifier.Notifications.Single();
        Assert.AreEqual(NotifyUsersTask.MaxTitleLength, notification.Title.Length);
        Assert.AreEqual(200, notification.Title.Length);
        Assert.AreEqual(2000, notification.Message.Length);
    }

    [TestMethod]
    public async Task Notifier_exception_fails_the_iteration()
    {
        _notifier.FailWith = _ => new InvalidOperationException("hub down");

        List<ExecResult> results = await RunAsync(Config("Backup"));

        Assert.IsFalse(results.Single().Result);
    }

    [TestMethod]
    public async Task Cancellation_propagates_and_is_not_a_failure()
    {
        using CancellationTokenSource cts = new();
        _notifier.FailWith = _ =>
        {
            cts.Cancel();
            return new OperationCanceledException(cts.Token);
        };

        await Assert.ThrowsAsync<OperationCanceledException>(() => RunAsync(Config("Backup"), cts.Token));
    }
}
