// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;

namespace OSRobot.Server.Plugins.MemoryEvent;

public class MemoryEventPlugin : IPlugin
{
    public string Id => "MemoryEvent";

    public string Title => Resource.TxtMemoryEvent;

    public EnumPluginType PluginType => EnumPluginType.Event;

    public List<DynamicDataSample> SampleDynamicData
    {
        get
        {
            List<DynamicDataSample> Samples = CommonDynamicData.BuildStandardDynamicDataSamples("MemoryEvent event 1");
            Samples.Add(new DynamicDataSample("MemoryUsagePercentage", Resource.TxtDynDataMemoryUsagePercentage, "45"));
            return Samples;
        }
    }

    public IPluginInstance GetInstance() => new MemoryEvent();

    public IPluginInstanceConfig GetPluginDefaultConfig() => new MemoryEventConfig();

    public EnumOSPlatform SupportedOSPlatforms => EnumOSPlatform.All;
}
