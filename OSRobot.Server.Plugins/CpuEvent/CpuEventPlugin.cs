// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;

namespace OSRobot.Server.Plugins.CpuEvent;

public class CpuEventPlugin : IPlugin
{
    public string Id => "CpuEvent";

    public string Title => Resource.TxtCpuEvent;

    public EnumPluginType PluginType => EnumPluginType.Event;

    public List<DynamicDataSample> SampleDynamicData
    {
        get
        {
            List<DynamicDataSample> samples = CommonDynamicData.BuildStandardDynamicDataSamples("CpuEvent event 1");
            samples.Add(new DynamicDataSample("CpuUsagePercentage", Resource.TxtDynDataCpuUsagePercentage, "45"));
            return samples;
        }
    }

    public IPluginInstance GetInstance() => new CpuEvent();
    
    public IPluginInstanceConfig GetPluginDefaultConfig() => new CpuEventConfig();
    
    public EnumOSPlatform SupportedOSPlatforms => EnumOSPlatform.Windows;
}
