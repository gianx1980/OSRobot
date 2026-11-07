// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Plugins.ServiceProcessWatchEvent;

/// <summary>A process as listed at a check. The name tells a reused process id apart from the process that had it before.</summary>
public readonly record struct ProcessInfo(int ProcessId, string ProcessName);

public enum ProcessChangeKind
{
    Started,
    Exited
}

/// <param name="ExitCode">Null for a start, and when the exit code can't be read.</param>
public sealed record ProcessStateChange(int ProcessId, string ProcessName, ProcessChangeKind Kind, int? ExitCode) : WatchChange;

/// <summary>The processes of the machine, and what is needed to read the exit code of a process after it has ended.</summary>
public interface IProcessSource : IDisposable
{
    IReadOnlyList<ProcessInfo> GetProcesses();

    /// <summary>Starts keeping what is needed to read the exit code of this running process; does nothing where that isn't possible.</summary>
    void Track(ProcessInfo process);

    /// <summary>Stops tracking a process that has ended.</summary>
    /// <returns>Its exit code, or null when it wasn't tracked or the code can't be read.</returns>
    int? TakeExitCode(ProcessInfo process);
}

/// <summary>
/// Reports the processes matching the name pattern that started or exited since the previous check. A process
/// that starts and exits between two checks is not seen.
/// </summary>
public sealed class ProcessWatcher(ServiceProcessWatchEventConfig config, IProcessSource source) : IWatcher
{
    private readonly ServiceProcessWatchEventConfig _config = config;
    private readonly IProcessSource _source = source;
    private readonly NamePattern _pattern = new(config.NamePattern);

    // Null until the baseline is taken.
    private HashSet<ProcessInfo>? _previous;

    public IReadOnlyList<WatchChange> Check()
    {
        HashSet<ProcessInfo> current = [.. _source.GetProcesses().Where(IsMatch)];
        List<WatchChange> changes = [];

        if (_previous == null)
        {
            if (_config.OnProcessExited)
            {
                foreach (ProcessInfo process in current)
                    _source.Track(process);
            }
        }
        else
        {
            // Exits first: when a process id is reused, the old program ended before the new one started.
            foreach (ProcessInfo process in _previous.Where(p => !current.Contains(p)))
            {
                if (_config.OnProcessExited)
                    changes.Add(new ProcessStateChange(process.ProcessId, process.ProcessName, ProcessChangeKind.Exited, _source.TakeExitCode(process)));
            }

            foreach (ProcessInfo process in current.Where(p => !_previous.Contains(p)))
            {
                if (_config.OnProcessExited)
                    _source.Track(process);

                if (_config.OnProcessStarted)
                    changes.Add(new ProcessStateChange(process.ProcessId, process.ProcessName, ProcessChangeKind.Started, null));
            }
        }

        _previous = current;
        return changes;
    }

    // Process names have no extension, but it is often typed.
    private bool IsMatch(ProcessInfo process) => _pattern.IsMatch(process.ProcessName) || _pattern.IsMatch(process.ProcessName + ".exe");

    public void Dispose() => _source.Dispose();
}
