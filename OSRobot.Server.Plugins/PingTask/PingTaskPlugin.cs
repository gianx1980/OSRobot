// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;

namespace OSRobot.Server.Plugins.PingTask;

public class PingTaskPlugin : IPlugin
{
    public string Id => "PingTask";

    public string Title => Resource.TxtPingTask;

    public EnumPluginType PluginType => EnumPluginType.Task;

    public List<DynamicDataSample> SampleDynamicData
    {
        get
        {
            List<DynamicDataSample> Samples = CommonDynamicData.BuildStandardDynamicDataSamples("Ping task 1");
            Samples.Add(new DynamicDataSample("ThresholdSuccessRate", Resource.TxtThresholdSuccessRate, @"50%"));
            return Samples;
        }
    }

    public IPluginInstance GetInstance() => new PingTask();

    public IPluginInstanceConfig GetPluginDefaultConfig() => new PingTaskConfig();

    public EnumOSPlatform SupportedOSPlatforms => EnumOSPlatform.All;
}
