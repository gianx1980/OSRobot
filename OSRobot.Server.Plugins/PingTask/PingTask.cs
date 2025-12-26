/*======================================================================================
    Copyright 2025 by Gianluca Di Bucci (gianx1980) (https://www.os-robot.com)

    This file is part of OSRobot.

    OSRobot is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    OSRobot is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with OSRobot.  If not, see <http://www.gnu.org/licenses/>.
======================================================================================*/

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using System.Net.NetworkInformation;

namespace OSRobot.Server.Plugins.PingTask;

public class PingTask : MultipleIterationTask
{
    private float _thresholdSuccessRate;

    protected override void RunMultipleIterationTask(int currentIteration)
    {
        PingTaskConfig config = (PingTaskConfig)_iterationTaskConfig;

        _thresholdSuccessRate = 0;
        int attemptSuccessCount = 0;
        Ping ping = new();

        for (int i = 1; i <= config.Attempts; i++)
        {
            try
            {
                _instanceLogger?.Info(this, $"Pinging host {config.Host} (Attempt: {i})...");
                PingReply reply = ping.Send(config.Host, config.Timeout);
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
