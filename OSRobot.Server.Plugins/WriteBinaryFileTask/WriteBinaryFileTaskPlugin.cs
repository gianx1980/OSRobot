// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;

namespace OSRobot.Server.Plugins.WriteBinaryFileTask;

public class WriteBinaryFileTaskPlugin : IPlugin
{
    public string Id => "WriteBinaryFileTask";

    public string Title => Resource.TxtWriteBinaryTaskFile;

    public EnumPluginType PluginType => EnumPluginType.Task;

    public List<DynamicDataSample> SampleDynamicData => CommonDynamicData.BuildStandardDynamicDataSamples("WriteBinaryFile task 1");

    public IPluginInstance GetInstance() => new WriteBinaryFileTask();

    public IPluginInstanceConfig GetPluginDefaultConfig() => new WriteBinaryFileTaskConfig();
    
    public EnumOSPlatform SupportedOSPlatforms => EnumOSPlatform.All;
}
