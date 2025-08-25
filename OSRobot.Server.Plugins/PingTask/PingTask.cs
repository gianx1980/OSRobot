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

using System.Net.NetworkInformation;
using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;

namespace OSRobot.Server.Plugins.PingTask;

public class PingTask : IterationTask
{
    private float _thresholdSuccessRate;

    protected override void RunIteration(int currentIteration)
    {
        PingTaskConfig tConfig = (PingTaskConfig)_iterationConfig;

        _thresholdSuccessRate = 0;
        int attemptSuccessCount = 0;
        Ping ping = new();

        for (int i = 0; i <= tConfig.Attempts; i++)
        {
            try
            {
                _instanceLogger?.Info(this, $"Pinging host {tConfig.Host} (Attempt: {i})...");
                PingReply reply = ping.Send(tConfig.Host, tConfig.Timeout);
                _instanceLogger?.Info(this, $"Status: {reply.Status}");

                if (reply.Status == IPStatus.Success)
                    attemptSuccessCount++;
            }
            catch (Exception ex)
            {
                _instanceLogger?.Error(this, $"Ping attempt failed", ex);
            }
        }

        _thresholdSuccessRate = (float)attemptSuccessCount / tConfig.Attempts;
    }

    private void PostIteration(int currentIteration, ExecResult result, DynamicDataSet dDataSet)
    {
        dDataSet.TryAdd("ThresholdSuccessRate", _thresholdSuccessRate);
    }

    protected override void PostIterationSucceded(int currentIteration, ExecResult result, DynamicDataSet dDataSet)
    {
        PostIteration(currentIteration, result, dDataSet);
    }

    protected override void PostIterationFailed(int currentIteration, ExecResult result, DynamicDataSet dDataSet)
    {
        PostIteration(currentIteration, result, dDataSet);
    }
}
