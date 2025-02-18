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
using OSRobot.Server.Core.DynamicData;

namespace OSRobot.Server.Plugins.DateTimeEvent;

public class DateTimeEventPlugin : IPlugin
{
    public string Id { get { return "DateTimeEvent"; } }
    public string Title { get { return Resource.TxtDateTimeEvent; } }
    public EnumPluginType PluginType { get { return EnumPluginType.Event;} }

    public List<DynamicDataSample> SampleDynamicData 
    { 
        get 
        {
            return CommonDynamicData.BuildStandardDynamicDataSamples("DateTime event 1");
        } 
    }

    public IPluginInstance GetInstance()
    {   
        return new DateTimeEvent();
    }

    public IPluginInstanceConfig GetPluginDefaultConfig()
    {
        return new DateTimeEventConfig();
    }

    public EnumOSPlatform SupportedOSPlatforms { get => EnumOSPlatform.All; }
}
