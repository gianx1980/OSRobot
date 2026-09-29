// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;

namespace OSRobot.Server.Plugins.FileSystemEvent;

public class FileSystemEventPlugin : IPlugin
{
    public string Id => "FileSystemEvent";

    public string Title => Resource.TxtFileSystemEvent;

    public EnumPluginType PluginType => EnumPluginType.Event;

    public List<DynamicDataSample> SampleDynamicData
    {
        get
        {
            List<DynamicDataSample> Samples = CommonDynamicData.BuildStandardDynamicDataSamples("File system event 1");
            Samples.Add(new DynamicDataSample(FileSystemEventCommon.DynDataKeyFullPathName, Resource.TxtDynDataFullPathName, @"C:\MyFolder\MyFile.txt"));
            Samples.Add(new DynamicDataSample(FileSystemEventCommon.DynDataKeyFileName, Resource.TxtDynDataFileName, "MyFile.txt"));
            Samples.Add(new DynamicDataSample(FileSystemEventCommon.DynDataKeyFileNameWithoutExtension, Resource.TxtDynDataFileNameWithoutExtension, @"MyFile"));
            Samples.Add(new DynamicDataSample(FileSystemEventCommon.DynDataKeyFileExtension, Resource.TxtDynDataFileExtension, @"txt"));
            Samples.Add(new DynamicDataSample(FileSystemEventCommon.DynDataKeyChangeType, Resource.TxtDynDataChangeType, @"Created"));
            return Samples;
        }
    }

    public IPluginInstance GetInstance() => new FileSystemEvent();
    
    public IPluginInstanceConfig GetPluginDefaultConfig() => new FileSystemEventConfig();

    public EnumOSPlatform SupportedOSPlatforms => EnumOSPlatform.All;
}
