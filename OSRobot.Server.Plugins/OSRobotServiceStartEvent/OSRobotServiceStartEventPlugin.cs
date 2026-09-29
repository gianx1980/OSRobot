// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;

namespace OSRobot.Server.Plugins.OSRobotServiceStartEvent;

public class OSRobotServiceStartEventPlugin : IPlugin
{
    public string Id => "OSRobotServiceStartEvent";

    public string Title => Resource.TxtOSRobotServiceStartEvent;

    public EnumPluginType PluginType => EnumPluginType.Event;

    public List<DynamicDataSample> SampleDynamicData => CommonDynamicData.BuildStandardDynamicDataSamples("OSRobot Robot service start event 1");

    public IPluginInstance GetInstance() => new OSRobotServiceStartEvent();

    public IPluginInstanceConfig GetPluginDefaultConfig() => new OSRobotServiceStartEventConfig();

    public EnumOSPlatform SupportedOSPlatforms => EnumOSPlatform.All;
}
