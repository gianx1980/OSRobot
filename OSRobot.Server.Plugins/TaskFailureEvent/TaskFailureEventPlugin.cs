// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;

namespace OSRobot.Server.Plugins.TaskFailureEvent;

public class TaskFailureEventPlugin : IPlugin
{
    public string Id => "TaskFailureEvent";

    public string Title => Resource.TxtTaskFailureEvent;

    public EnumPluginType PluginType => EnumPluginType.Event;

    public List<DynamicDataSample> SampleDynamicData
    {
        get
        {
            List<DynamicDataSample> samples = CommonDynamicData.BuildStandardDynamicDataSamples("Task failure event 1");
            samples.Add(new DynamicDataSample(TaskFailureEvent.FailedObjectID, Resource.TxtDynDataFailedObjectID, "12"));
            samples.Add(new DynamicDataSample(TaskFailureEvent.FailedObjectName, Resource.TxtDynDataFailedObjectName, "Nightly backup"));
            samples.Add(new DynamicDataSample(TaskFailureEvent.FailedObjectType, Resource.TxtDynDataFailedObjectType, "SQL Server backup"));
            samples.Add(new DynamicDataSample(TaskFailureEvent.FailedObjectFolder, Resource.TxtDynDataFailedObjectFolder, "Backups/Nightly"));
            samples.Add(new DynamicDataSample(TaskFailureEvent.FailureMessage, Resource.TxtDynDataFailureMessage, "The network path was not found."));
            samples.Add(new DynamicDataSample(TaskFailureEvent.FailedIterationIndex, Resource.TxtDynDataFailedIterationIndex, "0"));
            samples.Add(new DynamicDataSample(TaskFailureEvent.FailureDate, Resource.TxtDynDataFailureDate, "02/20/2021 18:30:00"));
            samples.Add(new DynamicDataSample(TaskFailureEvent.FailureCount, Resource.TxtDynDataFailureCount, "3"));
            samples.Add(new DynamicDataSample(TaskFailureEvent.Failures, Resource.TxtDynDataFailures, "ObjectID, ObjectName, ObjectType, Folder, Message, IterationIndex, FailureDate", true));
            return samples;
        }
    }

    public IPluginInstance GetInstance() => new TaskFailureEvent();

    public IPluginInstanceConfig GetPluginDefaultConfig() => new TaskFailureEventConfig();

    public EnumOSPlatform SupportedOSPlatforms => EnumOSPlatform.All;
}
