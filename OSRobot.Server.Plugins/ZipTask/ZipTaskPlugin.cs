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

namespace OSRobot.Server.Plugins.ZipTask;

public class ZipTaskPlugin : IPlugin
{
    public string Id { get { return "ZipTask"; } }

    public string Title { get { return Resource.TxtZipTask; } }

    public EnumPluginType PluginType { get { return EnumPluginType.Task; } }

    public List<DynamicDataSample> SampleDynamicData
    {
        get
        {
            return CommonDynamicData.BuildStandardDynamicDataSamples("Zip task 1");
        }
    }

    public IPluginInstance GetInstance()
    {
        return new ZipTask();
    }

    public IPluginInstanceConfig GetPluginDefaultConfig()
    {
        return new ZipTaskConfig();
    }

    public EnumOSPlatform SupportedOSPlatforms { get => EnumOSPlatform.All; }
}
