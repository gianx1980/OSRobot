/*======================================================================================
    Copyright 2025 by Gianluca Di Bucci (gianx1980) (https://www.os-robot.com)

    This file is part of OSRobot.

    OSRobot is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    OSRobot is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with OSRobot.  If not, see <http://www.gnu.org/licenses/>.
======================================================================================*/
using OSRobot.Server.Core;

namespace OSRobot.Server.Models.DTO.Robot;

public class PluginListItem
{ 
    public PluginListItem(string pluginId, string title, string pluginType, IPluginInstanceConfig configSample, EnumOSPlatform supportedOSPlatforms)
    {
        Id = pluginId;
        Title = title;
        Type = pluginType;
        ConfigSample = configSample;
        SupportedOSPlatforms = supportedOSPlatforms;
    }

    public string Id { get; }
    public string Title { get; }
    public string Type { get; }
    public object ConfigSample { get; }
    public EnumOSPlatform SupportedOSPlatforms { get; }
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

            return platforms.ToArray();   
        } 
    }
}
