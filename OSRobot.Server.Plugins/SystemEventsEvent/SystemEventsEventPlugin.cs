// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;

namespace OSRobot.Server.Plugins.SystemEventsEvent;


public class SystemEventsEventPlugin : IPlugin
{
    public string Id => "SystemEventsEvent";

    public string Title => Resource.TxtSystemEventsEvent;

    public EnumPluginType PluginType => EnumPluginType.Event;

    public List<DynamicDataSample> SampleDynamicData
    {
        get
        {
            List<DynamicDataSample> Samples = CommonDynamicData.BuildStandardDynamicDataSamples("System events plugin 1");
            Samples.Add(new DynamicDataSample(SystemEventsEventCommon.DynDataKeyEventCode, Resource.TxtEventCode, SystemEventsEventCommon.EventCodeTimeChanged));
            return Samples;
        }
    }

    public IPluginInstance GetInstance() => new SystemEventsEvent();

    public IPluginInstanceConfig GetPluginDefaultConfig() => new SystemEventsEventConfig();
    
    public EnumOSPlatform SupportedOSPlatforms => EnumOSPlatform.All;
}
