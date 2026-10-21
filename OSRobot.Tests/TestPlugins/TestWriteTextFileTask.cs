// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging.Abstract;
using OSRobot.Server.Plugins.WriteTextFileTask;

namespace OSRobot.Tests.TestPlugins;

[TestClass]
public sealed class TestWriteTextFileTask
{
    private string _folder = null!;
    private string _filePath = null!;

    [TestInitialize]
    public void CreateFolder()
    {
        // One folder per test: methods run in parallel.
        _folder = Path.Combine(Path.GetTempPath(), "OSRobotWriteTextFileTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
        _filePath = Path.Combine(_folder, "out.txt");
    }

    [TestCleanup]
    public void DeleteFolder()
    {
        try { Directory.Delete(_folder, true); } catch { /* best effort */ }
    }

    private WriteTextFileTaskConfig AppendConfig(int rows, bool overwrite = false, bool addHeaderIfEmpty = false) => new()
    {
        Id = 1,
        Name = "Write text file 1",
        FilePath = _filePath,
        TaskType = WriteTextFileTaskType.AppendRow,
        ColumnsDefinition = [new WriteTextFileColumnDefinition { HeaderTitle = "Name", FieldValue = "new" }],
        AddHeaderIfEmpty = addHeaderIfEmpty,
        OverwriteIfExists = overwrite,
        PluginIterationMode = IterationMode.IterateExactNumber,
        IterationsCount = rows
    };

    private static async Task<ExecResult> RunAsync(WriteTextFileTaskConfig config)
    {
        WriteTextFileTask task = new() { Config = config, ParentFolder = Common.CreateRootFolder() };
        Common.ConfigureLogPath();
        (DynamicDataChain chain, DynamicDataSet set, IPluginInstanceLogger logger) = Common.GetTaskDefaultParameters(task);
        InstanceExecResult result = await task.RunAsync(chain, set, null, logger, CancellationToken.None);
        return result.ExecResults.Single();
    }

    [TestMethod]
    public async Task Existing_file_is_appended_to_by_default()
    {
        File.WriteAllLines(_filePath, ["old"]);

        ExecResult result = await RunAsync(AppendConfig(rows: 2));

        Assert.IsTrue(result.Result);
        CollectionAssert.AreEqual(new[] { "old", "new", "new" }, File.ReadAllLines(_filePath));
    }

    [TestMethod]
    public async Task Existing_file_is_replaced_by_all_rows_of_the_run_when_overwrite_is_on()
    {
        File.WriteAllLines(_filePath, ["old", "older"]);

        ExecResult result = await RunAsync(AppendConfig(rows: 3, overwrite: true));

        Assert.IsTrue(result.Result);
        CollectionAssert.AreEqual(new[] { "new", "new", "new" }, File.ReadAllLines(_filePath));
    }

    [TestMethod]
    public async Task Overwritten_file_gets_the_header_again()
    {
        File.WriteAllLines(_filePath, ["Name", "old"]);

        await RunAsync(AppendConfig(rows: 1, overwrite: true, addHeaderIfEmpty: true));

        CollectionAssert.AreEqual(new[] { "Name", "new" }, File.ReadAllLines(_filePath));
    }

    [TestMethod]
    public async Task Overwrite_creates_the_file_when_missing()
    {
        await RunAsync(AppendConfig(rows: 2, overwrite: true));

        CollectionAssert.AreEqual(new[] { "new", "new" }, File.ReadAllLines(_filePath));
    }
}
