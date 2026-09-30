// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;

namespace OSRobot.Server.Plugins.DiskSpaceEvent;

public class DiskSpaceEventPlugin : IPlugin
{
    public string Id { get { return "DiskSpaceEvent"; } }

    public string Title { get { return Resource.TxtDiskSpaceEvent; } }

    public EnumPluginType PluginType { get { return EnumPluginType.Event; } }

    public List<DynamicDataSample> SampleDynamicData
    {
        get
        {
            List<DynamicDataSample> Samples = CommonDynamicData.BuildStandardDynamicDataSamples("DiskSpace event 1");
            Samples.Add(new DynamicDataSample("DiskName", Resource.TxtDynDataDiskName, @"C:\"));
            Samples.Add(new DynamicDataSample("DiskSpaceBytes", Resource.TxtDynDataDiskSpaceByte, "500"));
            return Samples;
        }
    }

    public IPluginInstance GetInstance()
    {
        return new DiskSpaceEvent();
    }

    public IPluginInstanceConfig GetPluginDefaultConfig()
    {
        return new DiskSpaceEventConfig();
    }

    public EnumOSPlatform SupportedOSPlatforms { get => EnumOSPlatform.All; }
}
