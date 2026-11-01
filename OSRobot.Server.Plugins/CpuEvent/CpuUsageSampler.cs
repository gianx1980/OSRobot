// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Diagnostics;
using System.Globalization;
using System.Runtime.Versioning;

namespace OSRobot.Server.Plugins.CpuEvent;

/// <summary>
/// Total CPU usage of the machine, as a percentage of all its processors, measured between two calls.
/// .NET has no cross-platform API for it: each platform has its own sampler.
/// </summary>
public interface ICpuUsageSampler : IDisposable
{
    /// <summary>The usage since the previous call; null on the first call, which only takes the starting point.</summary>
    float? NextSample();
}

public static class CpuUsageSampler
{
    public static ICpuUsageSampler Create()
    {
        if (OperatingSystem.IsWindows())
            return new WindowsCpuUsageSampler();

        if (OperatingSystem.IsLinux())
            return new LinuxCpuUsageSampler();

        throw new PlatformNotSupportedException("The CPU event works on Windows and Linux only.");
    }
}

/// <summary>
/// The "% Processor Utility" performance counter: what Task Manager shows (it accounts for the processors'
/// actual frequency), so thresholds keep the meaning they had before Linux was supported.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsCpuUsageSampler : ICpuUsageSampler
{
    private readonly PerformanceCounter _counter = new("Processor information", "% processor utility", "_Total");
    private CounterSample? _previous;

    public float? NextSample()
    {
        CounterSample current = _counter.NextSample();
        float? usage = _previous == null ? null : CounterSample.Calculate(_previous.Value, current);
        _previous = current;
        return usage;
    }

    public void Dispose() => _counter.Dispose();
}

/// <summary>
/// The first line of /proc/stat: the time all the processors spent in each state since boot. Usage is the
/// share of that time not spent idle between two readings, as top and htop compute it. In a container this
/// is the host's CPU, not the container's quota.
/// </summary>
public sealed class LinuxCpuUsageSampler(string procStatPath = "/proc/stat") : ICpuUsageSampler
{
    private readonly string _procStatPath = procStatPath;
    private CpuTimes? _previous;

    public float? NextSample()
    {
        CpuTimes current = CpuTimes.ParseProcStat(File.ReadAllText(_procStatPath));
        float? usage = _previous == null ? null : CpuTimes.UsagePercentage(_previous.Value, current);
        _previous = current;
        return usage;
    }

    public void Dispose() { }
}

/// <param name="Idle">Time spent idle, waiting for I/O included.</param>
/// <param name="Total">Time spent in any state.</param>
public readonly record struct CpuTimes(ulong Idle, ulong Total)
{
    /// <summary>
    /// Reads the "cpu" line of /proc/stat: user nice system idle iowait irq softirq steal [guest guest_nice].
    /// guest and guest_nice are left out: the kernel already counts them in user and nice.
    /// </summary>
    /// <exception cref="FormatException">There is no usable "cpu" line.</exception>
    public static CpuTimes ParseProcStat(string procStat)
    {
        string? line = procStat.Split('\n').FirstOrDefault(l => l.StartsWith("cpu ", StringComparison.Ordinal));
        if (line == null)
            throw new FormatException("/proc/stat has no \"cpu\" line.");

        ulong[] values = [.. line.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                 .Skip(1)
                                 .Take(8)
                                 .Select(v => ulong.Parse(v, CultureInfo.InvariantCulture))];
        if (values.Length < 4)
            throw new FormatException($"Unexpected /proc/stat \"cpu\" line: {line}");

        ulong idle = values[3] + (values.Length > 4 ? values[4] : 0);
        ulong total = 0;
        foreach (ulong value in values)
            total += value;

        return new CpuTimes(idle, total);
    }

    /// <summary>The busy share of the time elapsed between two readings, 0-100.</summary>
    public static float UsagePercentage(CpuTimes previous, CpuTimes current)
    {
        // No time elapsed, or the counters went back (they shouldn't): nothing to measure.
        if (current.Total <= previous.Total)
            return 0;

        ulong elapsed = current.Total - previous.Total;
        ulong idle = current.Idle >= previous.Idle ? Math.Min(current.Idle - previous.Idle, elapsed) : 0;

        return (float)(elapsed - idle) * 100f / elapsed;
    }
}
