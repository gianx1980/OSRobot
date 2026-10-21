// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.Notifications;

namespace OSRobot.Server.Plugins.NotifyUsersTask;

/// <summary>
/// Sends one notification per iteration to every open editor. Nobody listening is a success (the
/// notification is simply lost); a send error fails the iteration like any other task failure.
/// </summary>
public class NotifyUsersTask : MultipleIterationTask
{
    // A placeholder can expand to a whole recordset: cap what gets broadcast.
    public const int MaxTitleLength = 200;
    public const int MaxMessageLength = 2000;

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    protected override async Task RunMultipleIterationTaskAsync(int currentIteration)
    {
        NotifyUsersTaskConfig config = (NotifyUsersTaskConfig)_iterationTaskConfig;

        if (string.IsNullOrWhiteSpace(config.Title))
            throw new ApplicationException($"Task {config.Name}: the notification title is empty.");

        UserNotification notification = new(Truncate(config.Title, MaxTitleLength), Truncate(config.Message, MaxMessageLength),
                                             config.Level, DateTime.Now, config.Name);

        await Core.Core.UserNotifier.NotifyAsync(notification, _cancellationToken);

        if (config.Log)
            _instanceLogger.Info(this, $"Notification \"{notification.Title}\" sent to all connected editors.");
    }
}
