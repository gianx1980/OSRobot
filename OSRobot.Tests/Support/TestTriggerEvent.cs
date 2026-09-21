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
}

/// <summary>An event a test fires by hand with <see cref="Fire"/>, instead of waiting for a timer or file change.</summary>
public class TestTriggerEvent : IEvent
{
    private static readonly object _gate = new();
    private static readonly Dictionary<int, TestTriggerEvent> _live = [];

    public IFolder? ParentFolder { get; set; }
    public IPluginInstanceConfig Config { get; set; } = new TestTriggerEventConfig();
    public List<PluginInstanceConnection> Connections { get; set; } = [];

    [field: NonSerialized]
    public event EventTriggeredDelegate? EventTriggered;

    public void Init()
    {
        lock (_gate)
            _live[Config.Id] = this;
    }

    public void Destroy()
    {
        lock (_gate)
            if (_live.TryGetValue(Config.Id, out TestTriggerEvent? current) && ReferenceEquals(current, this))
                _live.Remove(Config.Id);
    }

    /// <summary>Raises the event with the given id, as the engine's event source would. Returns false if it isn't live.</summary>
    public static bool Fire(int eventId)
    {
        TestTriggerEvent? target;
        lock (_gate)
            _live.TryGetValue(eventId, out target);

        if (target == null)
            return false;

        DateTime now = DateTime.Now;
        DynamicDataSet dataSet = CommonDynamicData.BuildStandardDynamicDataSet(target, true, 0, now, now, 1);
        target.EventTriggered?.Invoke(target, new EventTriggeredEventArgs(dataSet, PluginInstanceLogger.GetLogger(target)));
        return true;
    }
}

public class TestTriggerEventPlugin : IPlugin
{
    public string Id => "TestTriggerEvent";
    public string Title => "Test trigger event";
    public EnumPluginType PluginType => EnumPluginType.Event;
    public List<DynamicDataSample> SampleDynamicData => [];
    public IPluginInstance GetInstance() => new TestTriggerEvent();
    public IPluginInstanceConfig GetPluginDefaultConfig() => new TestTriggerEventConfig();
    public EnumOSPlatform SupportedOSPlatforms => EnumOSPlatform.All;
}
