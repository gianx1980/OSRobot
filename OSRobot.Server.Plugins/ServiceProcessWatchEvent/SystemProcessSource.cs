// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace OSRobot.Server.Plugins.ServiceProcessWatchEvent;

/// <summary>
/// The processes of this machine. On Windows a tracked process is held by a handle with the least rights that
/// can read its exit code: the code stays readable after the process has ended, and Windows doesn't give the
/// process id to another process while the handle is open. Process.ExitCode can't be used: it opens the process
/// with all access rights, which fails on most processes the server didn't start. Elsewhere only the parent of a
/// process can read its exit status, so exit codes are never known.
/// </summary>
public sealed partial class SystemProcessSource : IProcessSource
{
    private const uint _processQueryLimitedInformation = 0x1000;

    private readonly Dictionary<ProcessInfo, SafeProcessHandle> _handles = [];

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial SafeProcessHandle OpenProcess(uint desiredAccess, [MarshalAs(UnmanagedType.Bool)] bool inheritHandle, int processId);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetExitCodeProcess(SafeProcessHandle process, out int exitCode);

    public IReadOnlyList<ProcessInfo> GetProcesses()
    {
        Process[] processes = Process.GetProcesses();
        List<ProcessInfo> result = new(processes.Length);

        foreach (Process process in processes)
        {
            using (process)
            {
                try
                {
                    result.Add(new ProcessInfo(process.Id, process.ProcessName));
                }
                catch (InvalidOperationException)
                {
                    // Ended while being listed: on Linux the name is read from /proc afterwards.
                }
            }
        }

        return result;
    }

    public void Track(ProcessInfo process)
    {
        if (!OperatingSystem.IsWindows() || _handles.ContainsKey(process))
            return;

        // Protected processes, and other users' processes without the rights, can't be opened: their exit code stays unknown.
        SafeProcessHandle handle = OpenProcess(_processQueryLimitedInformation, false, process.ProcessId);
        if (handle.IsInvalid)
            handle.Dispose();
        else
            _handles[process] = handle;
    }

    public int? TakeExitCode(ProcessInfo process)
    {
        if (!_handles.Remove(process, out SafeProcessHandle? handle))
            return null;

        using (handle)
            return GetExitCodeProcess(handle, out int exitCode) ? exitCode : null;
    }

    public void Dispose()
    {
        foreach (SafeProcessHandle handle in _handles.Values)
            handle.Dispose();

        _handles.Clear();
    }
}
