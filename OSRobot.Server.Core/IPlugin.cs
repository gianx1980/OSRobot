// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core.DynamicData;

namespace OSRobot.Server.Core;

public interface IPlugin
{
    public string Id { get; }
    public string Title { get; }
    public EnumPluginType PluginType { get; }
    public List<DynamicDataSample> SampleDynamicData { get; }
    IPluginInstance GetInstance();
    IPluginInstanceConfig GetPluginDefaultConfig();
    EnumOSPlatform SupportedOSPlatforms { get; }
 }
