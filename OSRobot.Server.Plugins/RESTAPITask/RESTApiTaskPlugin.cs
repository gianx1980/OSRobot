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

namespace OSRobot.Server.Plugins.RESTApiTask;

public class RESTApiTaskPlugin : IPlugin
{
    public string Id => "RESTApiTask"; 

    public string Title => Resource.TxtRESTApiTask; 

    public EnumPluginType PluginType => EnumPluginType.Task;

    public List<DynamicDataSample> SampleDynamicData
    {
        get
        {
            List<DynamicDataSample> Samples = CommonDynamicData.BuildStandardDynamicDataSamples("REST Api task 1");
            Samples.Add(new DynamicDataSample(RESTApiTaskCommon.DynDataKeyURL, Resource.TxtURL, Resource.TxtURLExample));
            Samples.Add(new DynamicDataSample(RESTApiTaskCommon.DynDataKeyRawContent, Resource.TxtRawContent, Resource.TxtRawContentExample));
            Samples.Add(new DynamicDataSample(RESTApiTaskCommon.DynDataKeyHttpResult, Resource.TxtHttpResult, Resource.TxtHttpResultExample));
            Samples.Add(new DynamicDataSample(RESTApiTaskCommon.DynDataKeyJsonPathData, Resource.TxtJsonPathToDataValue, "Value"));
            Samples.Add(new DynamicDataSample(CommonDynamicData.DefaultRecordsetName, Resource.TxtDynDataDefaultRecordset, Resource.TxtDynDataFieldXOfRecordsetsRow, true));
            return Samples;
        }
    }

    public IPluginInstance GetInstance() => new RESTApiTask();

    public IPluginInstanceConfig GetPluginDefaultConfig() => new RESTApiTaskConfig();

    public EnumOSPlatform SupportedOSPlatforms => EnumOSPlatform.All;
}
