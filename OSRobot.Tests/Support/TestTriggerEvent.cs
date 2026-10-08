// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging;

namespace OSRobot.Tests.Support;

public class TestTriggerEventConfig : IEventConfig
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public bool Log { get; set; } = true;

    /// <summary>Publish once from inside Init(), as OSRobotServiceStartEvent does.</summary>
    public bool FireOnInit { get; set; }
}

/// <summary>An event a test fires by hand with <see cref="Fire"/>, instead of waiting for a timer or file change.</summary>
public class TestTriggerEvent : IEvent
{
    private static readonly object _gate = new();
    private static readonly Dictionary<int, TestTriggerEvent> _live = [];

    private IEventSink? _sink;

    public IFolder? ParentFolder { get; set; }
    public IPluginInstanceConfig Config { get; set; } = new TestTriggerEventConfig();
    public List<PluginInstanceConnection> Connections { get; set; } = [];

    public void Init(IEventSink sink)
    {
        _sink = sink;

        lock (_gate)
            _live[Config.Id] = this;

        if (((TestTriggerEventConfig)Config).FireOnInit)
            Publish();
    }

    public void Destroy()
    {
        lock (_gate)
            if (_live.TryGetValue(Config.Id, out TestTriggerEvent? current) && ReferenceEquals(current, this))
                _live.Remove(Config.Id);
    }

    private bool Publish()
    {
        DateTime now = DateTime.Now;
        DynamicDataSet dataSet = CommonDynamicData.BuildStandardDynamicDataSet(this, true, 0, now, now, 1);
        return _sink?.Publish(this, dataSet, PluginInstanceLogger.GetLogger(this)) ?? false;
    }

    /// <summary>
    /// Publishes the event with the given id, as the event source would. Returns false if it isn't live
    /// or the engine ignored it.
    /// </summary>
    public static bool Fire(int eventId)
    {
        TestTriggerEvent? target;
        lock (_gate)
            _live.TryGetValue(eventId, out target);

        return target?.Publish() ?? false;
    }
}

public class TestTriggerEventPlugin : IPlugin
{
    public string Id => "TestTriggerEvent";
    public string Title => "Test trigger event";
    public EnumPluginType PluginType => EnumPluginType.Event;
    public List<DynamicDataSample> SampleDynamicData => CommonDynamicData.BuildStandardDynamicDataSamples("Trigger");
    public IPluginInstance GetInstance() => new TestTriggerEvent();
    public IPluginInstanceConfig GetPluginDefaultConfig() => new TestTriggerEventConfig();
    public EnumOSPlatform SupportedOSPlatforms => EnumOSPlatform.All;
}
