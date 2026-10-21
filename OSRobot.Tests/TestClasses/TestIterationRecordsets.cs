// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Data;
using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging.Abstract;
using OSRobot.Server.Plugins.FileSystemTask;
using OSRobot.Server.Plugins.ReadBinaryFileTask;
using OSRobot.Server.Plugins.ReadTextFileTask;

namespace OSRobot.Tests.TestClasses;

/// <summary>
/// Each iteration of a multi-iteration task must publish its own recordset. Regression: the plugins that
/// fill the task's shared default recordset reused one table for every iteration, so from the second
/// iteration on they either failed (columns added twice) or published the rows of all the iterations so far.
/// </summary>
[TestClass]
public sealed class TestIterationRecordsets
{
    private const int Iterations = 2;

    private static string CreateTestFolder(string name, int fileCount)
    {
        string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, name);
        if (Directory.Exists(folder))
            Directory.Delete(folder, true);
        Directory.CreateDirectory(folder);

        for (int i = 0; i < fileCount; i++)
            Common.WriteTestFile(Path.Combine(folder, $"file{i}.txt"), $"Content {i}");

        return folder;
    }

    private static async Task<List<ExecResult>> RunAsync(ITask task)
    {
        Common.ConfigureLogPath();
        (DynamicDataChain dynDataChain, DynamicDataSet dynDataSet, IPluginInstanceLogger logger) = Common.GetTaskDefaultParameters(task);

        task.Init();
        try
        {
            return (await task.RunAsync(dynDataChain, dynDataSet, 0, logger, CancellationToken.None)).ExecResults;
        }
        finally
        {
            task.Destroy();
        }
    }

    private static void AssertEachIterationHasItsOwnRecordset(List<ExecResult> results, int expectedRowsPerIteration)
    {
        Assert.HasCount(Iterations, results);
        Assert.IsTrue(results.All(r => r.Result), "Every iteration should succeed.");

        DataTable[] recordsets = [.. results.Select(r => (DataTable)r.Data[CommonDynamicData.DefaultRecordsetName])];
        Assert.AreNotSame(recordsets[0], recordsets[1], "Each iteration must publish its own recordset.");

        foreach (DataTable recordset in recordsets)
            Assert.AreEqual(expectedRowsPerIteration, recordset.Rows.Count, "An iteration's recordset must hold only that iteration's rows.");
    }

    [TestMethod]
    public async Task FileSystemTask_list_publishes_one_recordset_per_iteration()
    {
        string folder = CreateTestFolder("TestIterationRecordsetsList", fileCount: 2);

        FileSystemTask task = new()
        {
            ParentFolder = Common.CreateRootFolder(),
            Config = new FileSystemTaskConfig
            {
                Id = 1,
                Name = "List",
                Command = FileSystemTaskCommandType.List,
                ListFolderPath = folder,
                ListFiles = true,
                PluginIterationMode = IterationMode.IterateExactNumber,
                IterationsCount = Iterations
            }
        };

        AssertEachIterationHasItsOwnRecordset(await RunAsync(task), expectedRowsPerIteration: 2);
    }

    [TestMethod]
    public async Task ReadBinaryFileTask_publishes_one_recordset_per_iteration()
    {
        string folder = CreateTestFolder("TestIterationRecordsetsBinary", fileCount: 3);

        ReadBinaryFileTask task = new()
        {
            ParentFolder = Common.CreateRootFolder(),
            Config = new ReadBinaryFileTaskConfig
            {
                Id = 1,
                Name = "Read binary",
                FilePath = folder,
                PluginIterationMode = IterationMode.IterateExactNumber,
                IterationsCount = Iterations
            }
        };

        AssertEachIterationHasItsOwnRecordset(await RunAsync(task), expectedRowsPerIteration: 3);
    }

    [TestMethod]
    public async Task ReadTextFileTask_publishes_one_recordset_per_iteration()
    {
        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestIterationRecordsetsText.txt");
        File.WriteAllLines(filePath, ["A,B", "C,D", "E,F"]);

        ReadTextFileTask task = new()
        {
            ParentFolder = Common.CreateRootFolder(),
            Config = new ReadTextFileTaskConfig
            {
                Id = 1,
                Name = "Read text",
                FilePath = filePath,
                ReadAllTheRowsOption = true,
                PluginIterationMode = IterationMode.IterateExactNumber,
                IterationsCount = Iterations
            }
        };

        AssertEachIterationHasItsOwnRecordset(await RunAsync(task), expectedRowsPerIteration: 3);
    }
}
