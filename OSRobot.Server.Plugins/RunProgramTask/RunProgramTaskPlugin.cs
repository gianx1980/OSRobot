// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;

namespace OSRobot.Server.Plugins.RunProgramTask;

public class RunProgramTaskPlugin : IPlugin
{
    public string Id => "RunProgramTask";

    public string Title => Resource.TxtRunProgramTask;

    public EnumPluginType PluginType => EnumPluginType.Task;

    public List<DynamicDataSample> SampleDynamicData => CommonDynamicData.BuildStandardDynamicDataSamples("Run program task 1");
    public IPluginInstance GetInstance() => new RunProgramTask();

    public IPluginInstanceConfig GetPluginDefaultConfig() => new RunProgramTaskConfig();
    
    public EnumOSPlatform SupportedOSPlatforms { get => EnumOSPlatform.All; }
}
