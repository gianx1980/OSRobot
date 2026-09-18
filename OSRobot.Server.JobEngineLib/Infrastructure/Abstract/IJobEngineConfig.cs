/*======================================================================================
    Copyright 2025 by Gianluca Di Bucci (gianx1980) (https://www.os-robot.com)

    This file is part of OSRobot.

    OSRobot is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    OSRobot is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with OSRobot.  If not, see <http://www.gnu.org/licenses/>.
======================================================================================*/
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
