// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Diagnostics;
using System.Text.Json;
using OSRobot.Server.Core.Logging;
using OSRobot.Server.Core.Logging.Abstract;
using OSRobot.Server.Core.Notifications;
using OSRobot.Server.Core.Variables;
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
    public int MaxConcurrentTasks { get; set; } = 8;
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
    private readonly List<(int Id, string Name, JobGraph Content)> _folders = [];

    /// <param name="eventFiresOnInit">The event publishes once from inside Init(), like OSRobotServiceStartEvent.</param>
    public JobGraph(bool eventFiresOnInit = false)
    {
        _nodes.Add(new { workspaceItemConfig = new { pluginId = "TestTriggerEvent", id = EventId, name = "Trigger", fireOnInit = eventFiresOnInit } });
    }

    private JobGraph(bool withTrigger, bool eventFiresOnInit)
    {
        if (withTrigger)
            _nodes.Add(new { workspaceItemConfig = new { pluginId = "TestTriggerEvent", id = EventId, name = "Trigger", fireOnInit = eventFiresOnInit } });
    }

    /// <summary>The content of a subfolder: like a JobGraph, without the root's trigger event.</summary>
    public static JobGraph FolderContent() => new(withTrigger: false, eventFiresOnInit: false);

    /// <summary>Adds a trigger event, fired with TestTriggerEvent.Fire(id).</summary>
    public JobGraph Trigger(int id)
    {
        _nodes.Add(new { workspaceItemConfig = new { pluginId = "TestTriggerEvent", id, name = $"Trigger {id}", fireOnInit = false } });
        return this;
    }

    /// <summary>
    /// Adds a subfolder: its node here (holding its settings, as the editor saves it) and its own content.
    /// enabled null leaves "enabled" out of the node, as in jobs saved before folders could be disabled.
    /// </summary>
    public JobGraph Folder(int id, string name, JobGraph content, bool? enabled = null)
    {
        Dictionary<string, object> config = new() { ["pluginId"] = "Folder", ["id"] = id, ["name"] = name };
        if (enabled != null)
            config["enabled"] = enabled.Value;

        _nodes.Add(new { workspaceItemConfig = config });
        _folders.Add((id, name, content));
        return this;
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

    /// <summary>Adds an iterating task: each iteration shows up in the ProbeLog as "label#iterationIndex".</summary>
    public JobGraph IteratingTask(int id, string label, int iterations = 1, string value = "", int failOnIteration = -1,
                                  string iterationMode = "IterateExactNumber", string iterationObject = "", int rowsPerIteration = 0)
    {
        _nodes.Add(new
        {
            workspaceItemConfig = new
            {
                pluginId = "TestIteratingTask",
                id,
                name = label,
                label,
                value,
                failOnIteration,
                rowsPerIteration,
                pluginIterationMode = iterationMode,
                iterationObject,
                iterationsCount = iterations
            }
        });
        return this;
    }

    /// <summary>Adds an object of any plugin, given its workspaceItemConfig (which must include pluginId and id).</summary>
    public JobGraph Node(object workspaceItemConfig)
    {
        _nodes.Add(new { workspaceItemConfig });
        return this;
    }

    /// <summary>
    /// Connects two objects. With no conditions given, the target runs when the source succeeds. runMode and
    /// collectedResultRule are left out of the JSON when not given, like in jobs saved before they existed.
    /// dynamicDataCode and conditionValue apply to every condition (the field and the value it is compared with).
    /// </summary>
    public JobGraph Connect(int source, int target, int? waitSeconds = null, bool enabled = true,
                            string[]? executeOperators = null, string[]? dontExecuteOperators = null, string dynamicDataCode = "",
                            string? runMode = null, string? collectedResultRule = null, string conditionValue = "")
    {
        object[] Conditions(string[] operators) =>
            [.. operators.Select(o => new { dynamicDataCode, @operator = o, minValue = conditionValue, maxValue = string.Empty })];

        Dictionary<string, object?> connection = new()
        {
            ["source"] = source,
            ["target"] = target,
            ["enabled"] = enabled,
            ["waitSeconds"] = waitSeconds,
            ["executeConditions"] = Conditions(executeOperators ?? ["ObjectExecutes"]),
            ["dontExecuteConditions"] = Conditions(dontExecuteOperators ?? [])
        };

        if (runMode != null)
            connection["runMode"] = runMode;
        if (collectedResultRule != null)
            connection["collectedResultRule"] = collectedResultRule;

        _edges.Add(new { workspaceConnectionConfig = connection });
        return this;
    }

    public string ToJson()
    {
        var doc = new Dictionary<string, object>
        {
            ["folderTree"] = new[] { new { id = "0", label = "Root", icon = "folder", children = AddFolderData(doc: null, 0) } },
            ["lastId"] = 0
        };
        AddFolderData(doc, 0);
        return JsonSerializer.Serialize(doc);
    }

    // Writes folder_{id} for this graph and its subfolders into doc (when given); returns the folderTree children.
    private object[] AddFolderData(Dictionary<string, object>? doc, int id)
    {
        if (doc != null)
            doc[$"folder_{id}"] = new { id = id.ToString(), nodes = _nodes, edges = _edges };

        return [.. _folders.Select(f => new { id = f.Id.ToString(), label = f.Name, icon = "folder", children = f.Content.AddFolderData(doc, f.Id) })];
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

    public EngineHarness(JobGraph graph, bool serialExecution = false, int stopDrainTimeoutSeconds = 5, int maxConcurrentTasks = 8,
                         IUserNotifier? userNotifier = null, IVariableResolver? variables = null)
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
            MaxConcurrentTasks = maxConcurrentTasks,
            StopDrainTimeoutSeconds = stopDrainTimeoutSeconds
        };

        ProbeLog.Clear();
        Engine = new JobEngine(Log, Config, userNotifier, variables);
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
