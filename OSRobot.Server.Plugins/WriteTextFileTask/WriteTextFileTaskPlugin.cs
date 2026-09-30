// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;

namespace OSRobot.Server.Plugins.WriteTextFileTask;

public class WriteTextFileTaskPlugin : IPlugin
{
    public string Id => "WriteTextFileTask"; 

    public string Title => Resource.TxtWriteTextFileTask;

    public EnumPluginType PluginType => EnumPluginType.Task;

    public List<DynamicDataSample> SampleDynamicData
    {
        get
        {
            List<DynamicDataSample> Samples = CommonDynamicData.BuildStandardDynamicDataSamples("WriteTextFile task 1");
            Samples.Add(new DynamicDataSample(CommonDynamicData.DefaultRecordsetName, Resource.TxtDynDataDefaultRecordset, Resource.TxtDynDataFieldXOfRecordsetsRow, true));
            return Samples;
        }
    }

    public IPluginInstance GetInstance() => new WriteTextFileTask();

    public IPluginInstanceConfig GetPluginDefaultConfig() => new WriteTextFileTaskConfig();
    
    public EnumOSPlatform SupportedOSPlatforms => EnumOSPlatform.All;
}
