// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using System.Net.NetworkInformation;

namespace OSRobot.Server.Plugins.PingTask;

public class PingTask : MultipleIterationTask
{
    private float _thresholdSuccessRate;

    protected override async Task RunMultipleIterationTaskAsync(int currentIteration)
    {
        PingTaskConfig config = (PingTaskConfig)_iterationTaskConfig;

        _thresholdSuccessRate = 0;
        int attemptSuccessCount = 0;
        using Ping ping = new();

        for (int i = 1; i <= config.Attempts; i++)
        {
            try
            {
                _instanceLogger?.Info(this, $"Pinging host {config.Host} (Attempt: {i})...");
                PingReply reply = await ping.SendPingAsync(config.Host, config.Timeout).WaitAsync(_cancellationToken);
                _instanceLogger?.Info(this, $"Status: {reply.Status}");

                if (reply.Status == IPStatus.Success)
                    attemptSuccessCount++;
            }
            catch (Exception ex)
            {
                _instanceLogger?.Error(this, $"Ping attempt failed", ex);
            }
        }

        _thresholdSuccessRate = ((float)attemptSuccessCount / config.Attempts) * 100;
    }

    private void PostIteration(int currentIteration, ExecResult result, DynamicDataSet dDataSet)
    {
        dDataSet.TryAdd("ThresholdSuccessRate", _thresholdSuccessRate);
    }

    protected override void PostTaskSucceded(int currentIteration, ExecResult result, DynamicDataSet dDataSet)
    {
        PostIteration(currentIteration, result, dDataSet);
    }

    protected override void PostTaskFailed(int currentIteration, ExecResult result, DynamicDataSet dDataSet)
    {
        PostIteration(currentIteration, result, dDataSet);
    }
}
