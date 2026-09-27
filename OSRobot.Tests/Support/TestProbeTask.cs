using System.Collections.Concurrent;
using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;

namespace OSRobot.Tests.Support;

/// <summary>One recorded run of a <see cref="TestProbeTask"/>.</summary>
public sealed record ProbeEntry(string Label, DateTime Start, DateTime End, string Value, bool Cancelled);

/// <summary>Where <see cref="TestProbeTask"/> records what it did. Process-wide: clear it at the start of a test.</summary>
public static class ProbeLog
{
    private static readonly ConcurrentQueue<ProbeEntry> _entries = new();

    public static void Clear() => _entries.Clear();
    public static void Add(ProbeEntry entry) => _entries.Enqueue(entry);
    public static List<ProbeEntry> Entries => [.. _entries];
    public static ProbeEntry? Find(string label) => _entries.FirstOrDefault(e => e.Label == label);
}

public class TestProbeTaskConfig : ITaskConfig
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public bool Log { get; set; } = true;
    public IterationMode PluginIterationMode { get; set; } = IterationMode.IterateExactNumber;
    public string IterationObject { get; set; } = string.Empty;
    public int IterationsCount { get; set; } = 1;

    /// <summary>Recorded in the ProbeLog so a test can tell which task ran.</summary>
    public string Label { get; set; } = string.Empty;
    /// <summary>How long the task runs (cancellable), to model slow work.</summary>
    public int DelayMs { get; set; }
    public bool Fail { get; set; }
    /// <summary>Supports dynamic data, e.g. "{object[2].Value}", and is exposed as the "Value" output.</summary>
    [OSRobot.Server.Core.DynamicData.DynamicDataAttribute]
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// A controllable task for engine tests: waits DelayMs, optionally fails, then records itself in
/// <see cref="ProbeLog"/> and publishes Value as dynamic data.
/// </summary>
public class TestProbeTask : SingleIterationTask
{
    protected override async Task RunSingleIterationTaskAsync()
    {
        TestProbeTaskConfig config = (TestProbeTaskConfig)_taskConfig;
        DateTime start = DateTime.UtcNow;

        try
        {
            await Task.Delay(config.DelayMs, _cancellationToken);

            if (config.Fail)
                throw new InvalidOperationException("Probe task configured to fail.");

            ProbeLog.Add(new ProbeEntry(config.Label, start, DateTime.UtcNow, config.Value, Cancelled: false));
        }
        catch (OperationCanceledException)
        {
            ProbeLog.Add(new ProbeEntry(config.Label, start, DateTime.UtcNow, config.Value, Cancelled: true));
            throw;
        }
    }

    protected override void PostTaskSucceded(int currentIteration, ExecResult result, DynamicDataSet dDataSet)
    {
        dDataSet.TryAdd("Value", ((TestProbeTaskConfig)_taskConfig).Value);
    }
}

public class TestProbeTaskPlugin : IPlugin
{
    public string Id => "TestProbeTask";
    public string Title => "Test probe task";
    public EnumPluginType PluginType => EnumPluginType.Task;
    public List<DynamicDataSample> SampleDynamicData => [];
    public IPluginInstance GetInstance() => new TestProbeTask();
    public IPluginInstanceConfig GetPluginDefaultConfig() => new TestProbeTaskConfig();
    public EnumOSPlatform SupportedOSPlatforms => EnumOSPlatform.All;
}
