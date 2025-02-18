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

namespace OSRobot.Server.Plugins.SendEMailTask;

public class SendEMailTaskPlugin : IPlugin
{
    public string Id { get { return "SendEMailTask"; } }
    public string Title { get { return Resource.TxtSendEMailTask; } }
    public EnumPluginType PluginType { get { return EnumPluginType.Task; } }

    public List<DynamicDataSample> SampleDynamicData
    {
        get
        {
            return CommonDynamicData.BuildStandardDynamicDataSamples("Send email task 1");
        }
    }

    public IPluginInstance GetInstance()
    {
        return new SendEMailTask();
    }

    public IPluginInstanceConfig GetPluginDefaultConfig()
    {
        return new SendEMailTaskConfig();
    }

    public EnumOSPlatform SupportedOSPlatforms { get => EnumOSPlatform.All; }
}
