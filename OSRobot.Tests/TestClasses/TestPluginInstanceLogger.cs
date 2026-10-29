// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.Logging;
using OSRobot.Server.Core.Logging.Abstract;
using OSRobot.Tests.Support;

namespace OSRobot.Tests.TestClasses;

[TestClass]
public sealed class TestPluginInstanceLogger
{
    private const int Rounds = 20;
    private const int LoggersPerRound = 32;

    /// <summary>
    /// Regression: the instance counter in the log file name was read outside its lock, so loggers created
    /// at the same moment for the same task could get the same name (the ticks match too) and the second
    /// one failed with a sharing violation on its first write. The engine creates one per task run.
    /// </summary>
    [TestMethod]
    public void Loggers_created_at_the_same_moment_for_the_same_task_write_to_their_own_files()
    {
        Common.ConfigureLogPath();

        // A folder of its own, so the files counted below are only this test's.
        Folder root = Common.CreateRootFolder();
        Folder folder = new() { ParentFolder = root, Config = new FolderConfig { Id = Random.Shared.Next(1_000_000, int.MaxValue), Name = "Loggers" } };
        TestProbeTask task = new() { ParentFolder = folder, Config = new TestProbeTaskConfig { Id = 1, Name = "Probe" } };
        string logFolder = Path.Combine(PluginInstanceLogger.LogPath, folder.GetPhysicalFullPath());

        try
        {
            List<Exception> errors = [];
            for (int round = 0; round < Rounds; round++)
            {
                using Barrier start = new(LoggersPerRound);
                Thread[] threads = [.. Enumerable.Range(0, LoggersPerRound).Select(_ => new Thread(() =>
                {
                    start.SignalAndWait();
                    try
                    {
                        IPluginInstanceLogger logger = PluginInstanceLogger.GetLogger(task);
                        logger.TaskStarted(task);
                    }
                    catch (Exception ex)
                    {
                        lock (errors)
                            errors.Add(ex);
                    }
                }))];

                foreach (Thread thread in threads)
                    thread.Start();
                foreach (Thread thread in threads)
                    thread.Join();
            }

            Assert.IsEmpty(errors, errors.FirstOrDefault()?.Message);
            string[] files = Directory.GetFiles(logFolder);
            Assert.HasCount(Rounds * LoggersPerRound, files, "Every logger must have its own file.");

            // Matching ticks are rare, so a shared file is too; a repeated counter, which makes it possible, is not.
            string[] counters = [.. files.Select(f => Path.GetFileNameWithoutExtension(f).Split('_')[^1])];
            Assert.HasCount(counters.Length, counters.Distinct(), "The instance counter in the file name must never repeat.");
        }
        finally
        {
            try { Directory.Delete(logFolder, true); } catch (IOException) { }
        }
    }
}
