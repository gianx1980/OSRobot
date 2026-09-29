// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;

namespace OSRobot.Server.Plugins.ReadTextFileTask;

public class ReadTextFileTaskPlugin : IPlugin
{
    public string Id => "ReadTextFileTask";

    public string Title => Resource.TxtReadTextFileTask;

    public EnumPluginType PluginType => EnumPluginType.Task;

    public List<DynamicDataSample> SampleDynamicData
    {
        get
        {
            List<DynamicDataSample> Samples = CommonDynamicData.BuildStandardDynamicDataSamples("ReadTextFile task 1");
            Samples.Add(new DynamicDataSample(CommonDynamicData.DefaultRecordsetName, Resource.TxtDynDataDefaultRecordset, Resource.TxtDynDataFieldXOfRecordsetsRow, true));
            return Samples;
        }
    }

    public IPluginInstance GetInstance() => new ReadTextFileTask();

    public IPluginInstanceConfig GetPluginDefaultConfig() => new ReadTextFileTaskConfig();

    public EnumOSPlatform SupportedOSPlatforms => EnumOSPlatform.All;
}
