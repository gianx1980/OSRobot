// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;

namespace OSRobot.Server.Plugins.DateTimeEvent;

public class DateTimeEventPlugin : IPlugin
{
    public string Id { get { return "DateTimeEvent"; } }
    public string Title { get { return Resource.TxtDateTimeEvent; } }
    public EnumPluginType PluginType { get { return EnumPluginType.Event;} }

    public List<DynamicDataSample> SampleDynamicData 
    { 
        get 
        {
            return CommonDynamicData.BuildStandardDynamicDataSamples("DateTime event 1");
        } 
    }

    public IPluginInstance GetInstance()
    {   
        return new DateTimeEvent();
    }

    public IPluginInstanceConfig GetPluginDefaultConfig()
    {
        return new DateTimeEventConfig();
    }

    public EnumOSPlatform SupportedOSPlatforms { get => EnumOSPlatform.All; }
}
