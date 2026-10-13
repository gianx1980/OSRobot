// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using System.Diagnostics;

namespace OSRobot.Server.Plugins.RunProgramTask;

public class RunProgramTask : MultipleIterationTask
{
    protected override async Task RunMultipleIterationTaskAsync(int currentIteration)
    {
        RunProgramTaskConfig config = (RunProgramTaskConfig)_iterationTaskConfig;

        if (!Server.Core.Core.IsExecutablePathAllowed(config.ProgramPath))
            throw new ApplicationException($"'{config.ProgramPath}' is not in the configured AppSettings:JobEngineConfig:RunProgramAllowedExecutablePaths allowlist. See SECURITY.md.");

        ProcessStartInfo pInfo = new(config.ProgramPath, config.Parameters);
        string defaultWorkingFolder = Path.GetDirectoryName(config.ProgramPath) ?? string.Empty;
        pInfo.WorkingDirectory = string.IsNullOrEmpty(config.WorkingFolder) ? defaultWorkingFolder : config.WorkingFolder;
        _instanceLogger.Info(this, $"Running program: {config.ProgramPath} Parameters: {config.Parameters} Working folder: {pInfo.WorkingDirectory}");

        using Process? newProc = Process.Start(pInfo);
        // Must throw, not return: returning normally would make the base class record this
        // iteration as a success, so a program that never launched would look like it ran.
        if (newProc == null)
            throw new ApplicationException($"Run program failed: Process.Start returned null for '{config.ProgramPath}'.");

        try
        {
            await newProc.WaitForExitAsync(_cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Cancelling the wait doesn't end the program: kill it, or it would outlive the task.
            KillProcessTree(newProc);
            throw;
        }

        if (config.FailOnNonZeroExitCode && newProc.ExitCode != 0)
            throw new ApplicationException($"Program '{config.ProgramPath}' exited with code {newProc.ExitCode}.");

        _instanceLogger.Info(this, $"Program exited with code {newProc.ExitCode}");
    }

    private void KillProcessTree(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
            _instanceLogger.Info(this, $"Program killed (process id {process.Id}): task cancelled.");
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            // Already exited, or the OS refused: nothing more can be done here.
            _instanceLogger.Info(this, $"Could not kill program (process id {process.Id}): {ex.Message}");
        }
    }
}
