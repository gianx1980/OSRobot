// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;

namespace OSRobot.Server.Plugins.ServiceProcessWatchEvent;

public class ServiceProcessWatchEventPlugin : IPlugin
{
    public string Id => "ServiceProcessWatchEvent";

    public string Title => Resource.TxtServiceProcessWatchEvent;

    public EnumPluginType PluginType => EnumPluginType.Event;

    public List<DynamicDataSample> SampleDynamicData
    {
        get
        {
            List<DynamicDataSample> samples = CommonDynamicData.BuildStandardDynamicDataSamples("Service / process watch event 1");
            samples.Add(new DynamicDataSample(ServiceProcessWatchEvent.ServiceName, Resource.TxtDynDataServiceName, "Spooler"));
            samples.Add(new DynamicDataSample(ServiceProcessWatchEvent.ServiceDisplayName, Resource.TxtDynDataServiceDisplayName, "Print Spooler"));
            samples.Add(new DynamicDataSample(ServiceProcessWatchEvent.ServiceState, Resource.TxtDynDataServiceState, "Stopped"));
            samples.Add(new DynamicDataSample(ServiceProcessWatchEvent.PreviousServiceState, Resource.TxtDynDataPreviousServiceState, "Running"));
            samples.Add(new DynamicDataSample(ServiceProcessWatchEvent.ProcessName, Resource.TxtDynDataProcessName, "notepad"));
            samples.Add(new DynamicDataSample(ServiceProcessWatchEvent.ProcessId, Resource.TxtDynDataProcessId, "4242"));
            samples.Add(new DynamicDataSample(ServiceProcessWatchEvent.ProcessChange, Resource.TxtDynDataProcessChange, "Exited"));
            samples.Add(new DynamicDataSample(ServiceProcessWatchEvent.ExitCode, Resource.TxtDynDataExitCode, "0"));
            return samples;
        }
    }

    public IPluginInstance GetInstance() => new ServiceProcessWatchEvent();

    public IPluginInstanceConfig GetPluginDefaultConfig() => new ServiceProcessWatchEventConfig();

    // Services exist on Windows only; processes are listed the same way on Linux. macOS is untested.
    public EnumOSPlatform SupportedOSPlatforms => EnumOSPlatform.Windows | EnumOSPlatform.Linux;
}
