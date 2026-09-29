// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;

namespace OSRobot.Server.Plugins.SendEMailTask;

public class SendEMailTaskPlugin : IPlugin
{
    public string Id => "SendEMailTask";

    public string Title => Resource.TxtSendEMailTask;

    public EnumPluginType PluginType => EnumPluginType.Task; 

    public List<DynamicDataSample> SampleDynamicData => CommonDynamicData.BuildStandardDynamicDataSamples("Send email task 1");

    public IPluginInstance GetInstance() => new SendEMailTask();

    public IPluginInstanceConfig GetPluginDefaultConfig() => new SendEMailTaskConfig();

    public EnumOSPlatform SupportedOSPlatforms => EnumOSPlatform.All;
}
