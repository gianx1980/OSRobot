// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;

namespace OSRobot.Server.Plugins.ZipTask;

public class ZipTaskPlugin : IPlugin
{
    public string Id => "ZipTask";

    public string Title => Resource.TxtZipTask;

    public EnumPluginType PluginType => EnumPluginType.Task;

    public List<DynamicDataSample> SampleDynamicData => CommonDynamicData.BuildStandardDynamicDataSamples("Zip task 1");
    public IPluginInstance GetInstance() => new ZipTask();
    
    public IPluginInstanceConfig GetPluginDefaultConfig() => new ZipTaskConfig();
    
    public EnumOSPlatform SupportedOSPlatforms => EnumOSPlatform.All;
}
