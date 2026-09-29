// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;

namespace OSRobot.Server.Plugins.UnzipTask;

public class UnzipTaskPlugin : IPlugin
{
    public string Id => "UnzipTask";

    public string Title => Resource.TxtUnzipTask;

    public EnumPluginType PluginType => EnumPluginType.Task;

    public List<DynamicDataSample> SampleDynamicData => CommonDynamicData.BuildStandardDynamicDataSamples("Unzip task 1");

    public IPluginInstance GetInstance() => new UnzipTask();

    public IPluginInstanceConfig GetPluginDefaultConfig() => new UnzipTaskConfig();
    
    public EnumOSPlatform SupportedOSPlatforms => EnumOSPlatform.All;
}
