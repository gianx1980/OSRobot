// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.JobEngineLib.Infrastructure.Abstract;

public interface IJobEngineConfig
{
    public string LogPath { get; set; }
    public string DataPath { get; set; }
    /// <summary>
    /// When true, the engine runs one task at a time (a single queue worker, regardless of
    /// MaxConcurrentTasks), and a manual task start returns only once its whole run has finished.
    /// </summary>
    public bool SerialExecution { get; set; }

    /// <summary>
    /// How many tasks may run at the same time, across all jobs. Tasks beyond this wait in the
    /// queue, in the order they were triggered. 0 or less means the default (Constants.DefaultMaxConcurrentTasks).
    /// </summary>
    public int MaxConcurrentTasks { get; set; }

    public int CleanUpLogsOlderThanHours { get; set; }
    public int CleanUpLogsIntervalHours { get; set; }

    /// <summary>
    /// How long Stop() waits, after cancelling them, for the queued and running tasks to
    /// finish before proceeding with teardown anyway.
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
