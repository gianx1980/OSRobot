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
using OSRobot.Server.Core.Logging;

namespace OSRobot.Server.Core;

/// <summary>
/// Process-wide state set once at startup by JobEngine.Start() and read deep inside plugin/task
/// execution code that has no DI container reference of its own - the same pattern already used
/// for PluginInstanceLogger.LogPath.
/// </summary>
public static class Core
{
    /// <summary>
    /// When false, DynamicDataParser refuses to evaluate any [CODE] C# scripting expression.
    /// See AppSettings:JobEngineConfig:ScriptingEnabled / SECURITY.md.
    /// </summary>
    public static bool ScriptingEnabled { get; private set; } = true;

    private static string[] _allowedExecutablePaths = [];

    public static void Init(string logPath, bool scriptingEnabled = true, string? runProgramAllowedExecutablePaths = null)
    {
        PluginInstanceLogger.LogPath = logPath;
        ScriptingEnabled = scriptingEnabled;
        _allowedExecutablePaths =
        [
            .. (runProgramAllowedExecutablePaths ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        ];
    }

    /// <summary>
    /// True if no allowlist is configured (unrestricted - the default), or if
    /// <paramref name="programPath"/> matches an allowed entry exactly, or falls under an
    /// allowed entry that names a directory. See AppSettings:JobEngineConfig:
    /// RunProgramAllowedExecutablePaths / SECURITY.md - this only constrains RunProgramTask,
    /// not arbitrary [CODE] scripting, which can call Process.Start directly.
    /// </summary>
    public static bool IsExecutablePathAllowed(string programPath)
    {
        if (_allowedExecutablePaths.Length == 0)
            return true;

        string fullProgramPath;
        try
        {
            fullProgramPath = Path.GetFullPath(programPath);
        }
        catch
        {
            return false;
        }

        foreach (string allowedEntry in _allowedExecutablePaths)
        {
            string fullAllowedEntry;
            try
            {
                fullAllowedEntry = Path.GetFullPath(allowedEntry);
            }
            catch
            {
                continue;
            }

            if (string.Equals(fullProgramPath, fullAllowedEntry, StringComparison.OrdinalIgnoreCase))
                return true;

            // Entry names a directory: allow anything under it.
            if (Directory.Exists(fullAllowedEntry))
            {
                string directoryPrefix = fullAllowedEntry.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (fullProgramPath.StartsWith(directoryPrefix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }
}
