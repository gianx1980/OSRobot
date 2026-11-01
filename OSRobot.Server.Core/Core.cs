// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core.Logging;
using OSRobot.Server.Core.Notifications;
using OSRobot.Server.Core.Variables;

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

    /// <summary>
    /// Where NotifyUsersTask sends its notifications. The host passes its implementation to the
    /// JobEngine (in the server: a SignalR broadcast to every open editor); without one it is a no-op.
    /// </summary>
    public static IUserNotifier UserNotifier { get; private set; } = NullUserNotifier.Instance;

    /// <summary>
    /// The values of the {var['Name']} and {secret['Name']} placeholders, and the secrets redacted from
    /// the execution logs. The host passes its implementation to the JobEngine (in the server: the
    /// variables stored in config.db); without one nothing is defined.
    /// </summary>
    public static IVariableResolver Variables { get; private set; } = VariableSet.Empty;

    private static string[] _allowedExecutablePaths = [];

    public static void Init(string logPath, bool scriptingEnabled = true, string? runProgramAllowedExecutablePaths = null,
                            IUserNotifier? userNotifier = null, IVariableResolver? variables = null)
    {
        PluginInstanceLogger.LogPath = logPath;
        ScriptingEnabled = scriptingEnabled;
        UserNotifier = userNotifier ?? NullUserNotifier.Instance;
        Variables = variables ?? VariableSet.Empty;
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
