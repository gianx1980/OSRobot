// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Models.DTO.Robot;
using OSRobot.Server.Plugins.CpuEvent;

namespace OSRobot.Tests.TestPlugins;

[TestClass]
public sealed class TestCpuUsageSampler
{
    // A real first line of /proc/stat (WSL 2, Ubuntu).
    private const string RealProcStatLine = "cpu  1135 7 1079 77683 406 0 291 0 0 0";

    [TestMethod]
    public void Parses_the_cpu_line_of_proc_stat()
    {
        CpuTimes times = CpuTimes.ParseProcStat(RealProcStatLine + "\ncpu0 100 0 90 6400 30 0 20 0 0 0\nintr 12345\n");

        Assert.AreEqual(77683UL + 406UL, times.Idle, "idle + iowait");
        Assert.AreEqual(1135UL + 7 + 1079 + 77683 + 406 + 0 + 291 + 0, times.Total);
    }

    [TestMethod]
    public void Takes_the_total_line_not_a_single_processors()
    {
        CpuTimes times = CpuTimes.ParseProcStat("cpu0 1 1 1 1 1 1 1 1 0 0\ncpu  10 0 10 70 10 0 0 0 0 0\n");

        Assert.AreEqual(100UL, times.Total);
    }

    [TestMethod]
    public void Leaves_out_guest_time_already_counted_in_user_time()
    {
        // user 100 includes guest 30: counting both would inflate the busy time.
        CpuTimes times = CpuTimes.ParseProcStat("cpu  100 0 50 800 50 0 0 0 30 0");

        Assert.AreEqual(1000UL, times.Total);
        Assert.AreEqual(850UL, times.Idle);
    }

    [TestMethod]
    [DataRow("intr 1 2 3", DisplayName = "No cpu line")]
    [DataRow("cpu  1 2", DisplayName = "Too few fields")]
    public void A_file_without_a_usable_cpu_line_is_rejected(string content)
    {
        Assert.Throws<FormatException>(() => CpuTimes.ParseProcStat(content));
    }

    [TestMethod]
    public void Usage_is_the_busy_share_of_the_time_between_two_readings()
    {
        CpuTimes previous = new(Idle: 850, Total: 1000);
        CpuTimes current = new(Idle: 1150, Total: 1400);   // 400 elapsed, 300 of them idle

        Assert.AreEqual(25f, CpuTimes.UsagePercentage(previous, current), 0.001f);
    }

    [TestMethod]
    public void Usage_is_zero_when_no_time_elapsed_or_the_counters_went_back()
    {
        CpuTimes reading = new(Idle: 850, Total: 1000);

        Assert.AreEqual(0f, CpuTimes.UsagePercentage(reading, reading));
        Assert.AreEqual(0f, CpuTimes.UsagePercentage(reading, new CpuTimes(Idle: 10, Total: 20)));
    }

    [TestMethod]
    public void The_Linux_sampler_takes_a_starting_point_then_measures()
    {
        string procStat = Path.Combine(Path.GetTempPath(), $"OSRobotProcStat_{Guid.NewGuid():N}");
        try
        {
            File.WriteAllText(procStat, "cpu  100 0 50 800 50 0 0 0 0 0\n");
            using LinuxCpuUsageSampler sampler = new(procStat);
            Assert.IsNull(sampler.NextSample(), "The first reading is only the starting point");

            // 400 more units of time, 300 of them idle (250 idle + 50 iowait).
            File.WriteAllText(procStat, "cpu  150 0 100 1050 100 0 0 0 0 0\n");
            Assert.AreEqual(25f, sampler.NextSample()!.Value, 0.001f);
        }
        finally
        {
            File.Delete(procStat);
        }
    }

    [TestMethod]
    public void The_real_sampler_of_this_platform_measures_a_percentage()
    {
        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux())
            Assert.Inconclusive("The CPU event supports Windows and Linux only.");

        using ICpuUsageSampler sampler = CpuUsageSampler.Create();
        Assert.IsNull(sampler.NextSample());

        Thread.Sleep(500);
        float usage = sampler.NextSample()!.Value;

        Assert.IsTrue(usage is >= 0f and <= 100f, $"Usage {usage}");
    }

    [TestMethod]
    public void The_editor_lists_every_supported_platform()
    {
        CpuEventPlugin plugin = new();
        PluginListItem item = new(plugin.Id, plugin.Title, "event", plugin.GetPluginDefaultConfig(), plugin.SupportedOSPlatforms);

        CollectionAssert.AreEqual(new[] { "Windows", "Linux" }, item.SupportedOSPlatformList);
    }
}
