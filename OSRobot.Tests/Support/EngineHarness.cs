using System.Diagnostics;
using System.Text.Json;
using OSRobot.Server.Core.Logging;
using OSRobot.Server.Core.Logging.Abstract;
using OSRobot.Server.JobEngineLib;
using OSRobot.Server.JobEngineLib.Infrastructure.Abstract;

namespace OSRobot.Tests.Support;

/// <summary>Records what the engine logs, so tests can assert that a scenario produced no errors.</summary>
public sealed class CapturingAppLogger : IAppLogger
{
    private readonly object _gate = new();
    private readonly List<string> _errors = [];
    private readonly List<string> _warnings = [];

    public IReadOnlyList<string> Errors { get { lock (_gate) return [.. _errors]; } }
    public IReadOnlyList<string> Warnings { get { lock (_gate) return [.. _warnings]; } }

    public void Info(string message) { }
    public void Info(string message, Exception ex) { }
    public void Error(string message) { lock (_gate) _errors.Add(message); }
    public void Error(string message, Exception ex) { lock (_gate) _errors.Add($"{message}: {ex.Message}"); }
    public void Warn(string message) { lock (_gate) _warnings.Add(message); }
    public void Warn(string message, Exception ex) { lock (_gate) _warnings.Add($"{message}: {ex.Message}"); }
}

public sealed class TestEngineConfig : IJobEngineConfig
{
    public string LogPath { get; set; } = string.Empty;
    public string DataPath { get; set; } = string.Empty;
    public bool SerialExecution { get; set; }
    public int CleanUpLogsOlderThanHours { get; set; }
    public int CleanUpLogsIntervalHours { get; set; }
    public int StopDrainTimeoutSeconds { get; set; } = 5;
    public bool ScriptingEnabled { get; set; } = true;
    public string RunProgramAllowedExecutablePaths { get; set; } = string.Empty;
}

/// <summary>Builds the jobs.json the engine loads: one event wired to a graph of <see cref="TestProbeTask"/>s.</summary>
public sealed class JobGraph
{
    public const int EventId = 1;

    private readonly List<object> _nodes = [];
    private readonly List<object> _edges = [];

    public JobGraph()
    {
        _nodes.Add(new { workspaceItemConfig = new { pluginId = "TestTriggerEvent", id = EventId, name = "Trigger" } });
    }

    /// <summary>Adds a probe task. Its label is what shows up in the ProbeLog.</summary>
    public JobGraph Task(int id, string label, int delayMs = 0, bool fail = false, string value = "", bool enabled = true)
    {
        _nodes.Add(new
        {
            workspaceItemConfig = new
            {
                pluginId = "TestProbeTask",
                id,
                name = label,
                enabled,
                label,
                delayMs,
                fail,
                value,
                iterationsCount = 1
            }
        });
        return this;
    }

    /// <summary>Connects two objects. With no conditions given, the target runs when the source succeeds.</summary>
    public JobGraph Connect(int source, int target, int? waitSeconds = null, bool enabled = true,
                            string[]? executeOperators = null, string[]? dontExecuteOperators = null)
    {
        static object[] Conditions(string[] operators) =>
            [.. operators.Select(o => new { dynamicDataCode = string.Empty, @operator = o, minValue = string.Empty, maxValue = string.Empty })];

        _edges.Add(new
        {
            workspaceConnectionConfig = new
            {
                source,
                target,
                enabled,
                waitSeconds,
                executeConditions = Conditions(executeOperators ?? ["ObjectExecutes"]),
                dontExecuteConditions = Conditions(dontExecuteOperators ?? [])
            }
        });
        return this;
    }

    public string ToJson()
    {
        var doc = new Dictionary<string, object>
        {
            ["folder_0"] = new { id = "0", nodes = _nodes, edges = _edges },
            ["folderTree"] = new[] { new { id = "0", label = "Root", icon = "folder", children = Array.Empty<object>() } },
            ["lastId"] = 0
        };
        return JsonSerializer.Serialize(doc);
    }
}

/// <summary>
/// A real JobEngine started on a temporary jobs.json. The engine keeps process-wide state
/// (Core.Init, the static ProbeLog and TestTriggerEvent registry), so tests using it must not run in parallel.
/// </summary>
public sealed class EngineHarness : IDisposable
{
    private readonly string _root;
    private readonly string _previousLogPath = PluginInstanceLogger.LogPath;
    private bool _stopped;

    public JobEngine Engine { get; }
    public CapturingAppLogger Log { get; } = new();
    public TestEngineConfig Config { get; }

    public EngineHarness(JobGraph graph, bool serialExecution = false, int stopDrainTimeoutSeconds = 5)
    {
        _root = Path.Combine(Path.GetTempPath(), "OSRobotEngineTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_root, "Data"));
        Directory.CreateDirectory(Path.Combine(_root, "Logs"));
        File.WriteAllText(Path.Combine(_root, "Data", "jobs.json"), graph.ToJson());

        Config = new TestEngineConfig
        {
            DataPath = Path.Combine(_root, "Data"),
            LogPath = Path.Combine(_root, "Logs"),
            SerialExecution = serialExecution,
            StopDrainTimeoutSeconds = stopDrainTimeoutSeconds
        };

        ProbeLog.Clear();
        Engine = new JobEngine(Log, Config);
        Engine.Start();
    }

    public bool Fire() => TestTriggerEvent.Fire(JobGraph.EventId);

    /// <summary>Stops the engine and returns how long Stop() took.</summary>
    public TimeSpan Stop()
    {
        Stopwatch sw = Stopwatch.StartNew();
        Engine.Stop();
        _stopped = true;
        return sw.Elapsed;
    }

    public static bool WaitFor(Func<bool> condition, TimeSpan timeout)
    {
        Stopwatch sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            if (condition())
                return true;
            Thread.Sleep(20);
        }
        return condition();
    }

    public void Dispose()
    {
        if (!_stopped)
            Engine.Stop();

        // JobEngine.Start() points the process-wide logger at this harness's temp folder: put it back
        // before deleting that folder, so later tests don't log into a directory that no longer exists.
        PluginInstanceLogger.LogPath = _previousLogPath;

        try { Directory.Delete(_root, true); } catch { /* best effort: log files may still be released */ }
    }
}
