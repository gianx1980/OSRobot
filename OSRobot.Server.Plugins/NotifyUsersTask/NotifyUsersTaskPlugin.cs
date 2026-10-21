// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;

namespace OSRobot.Server.Plugins.NotifyUsersTask;

public class NotifyUsersTaskPlugin : IPlugin
{
    public string Id => "NotifyUsersTask";

    public string Title => Resource.TxtNotifyUsersTask;

    public EnumPluginType PluginType => EnumPluginType.Task;

    public List<DynamicDataSample> SampleDynamicData => CommonDynamicData.BuildStandardDynamicDataSamples("Notification task 1");

    public IPluginInstance GetInstance() => new NotifyUsersTask();

    public IPluginInstanceConfig GetPluginDefaultConfig() => new NotifyUsersTaskConfig();

    public EnumOSPlatform SupportedOSPlatforms => EnumOSPlatform.All;
}
