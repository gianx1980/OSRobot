// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;

namespace OSRobot.Server.Models.DTO.Robot;

public class PluginListItem(string pluginId, string title, string pluginType, IPluginInstanceConfig configSample, EnumOSPlatform supportedOSPlatforms)
{
    public string Id { get; } = pluginId;
    public string Title { get; } = title;
    public string Type { get; } = pluginType;
    public object ConfigSample { get; } = configSample;
    public EnumOSPlatform SupportedOSPlatforms { get; } = supportedOSPlatforms;
    public string[] SupportedOSPlatformList { 
        get {
            List<string> platforms = [];

            if (SupportedOSPlatforms == EnumOSPlatform.All)
                platforms.Add("All platforms");
            else
            {
                if (SupportedOSPlatforms.HasFlag(EnumOSPlatform.Windows))
                    platforms.Add("Windows");
                else if (SupportedOSPlatforms.HasFlag(EnumOSPlatform.Linux))
                    platforms.Add("Linux");
                else if (SupportedOSPlatforms.HasFlag(EnumOSPlatform.MacOS))
                    platforms.Add("MacOs");

            }

            return [.. platforms];   
        } 
    }
}
