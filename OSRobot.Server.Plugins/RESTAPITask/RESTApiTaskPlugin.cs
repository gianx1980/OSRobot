// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

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
