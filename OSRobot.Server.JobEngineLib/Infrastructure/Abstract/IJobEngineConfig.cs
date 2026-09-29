// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.JobEngineLib.Infrastructure.Abstract;

public interface IJobEngineConfig
{
    public string LogPath { get; set; }
    public string DataPath { get; set; }
    public bool SerialExecution { get; set; }
    public int CleanUpLogsOlderThanHours { get; set; }
    public int CleanUpLogsIntervalHours { get; set; }

    /// <summary>
    /// How long Stop() waits for in-flight event/task dispatches and running tasks to
    /// drain (each, independently) before proceeding with teardown anyway. Used by
    /// both WaitForDispatchesToDrain() and WaitForRunningTasksToDrain().
    /// </summary>
    public int StopDrainTimeoutSeconds { get; set; }

    /// <summary>
    /// When false, [CODE] C# scripting expressions fail instead of executing. Default true
    /// (full power, unrestricted). See SECURITY.md.
    /// </summary>
    public bool ScriptingEnabled { get; set; }

    /// <summary>
    /// Comma-separated allowlist of executable paths/directories RunProgramTask may launch.
    /// Empty (default) means unrestricted. Only meaningful alongside ScriptingEnabled = false -
    /// see SECURITY.md.
    /// </summary>
    public string RunProgramAllowedExecutablePaths { get; set; }
}
