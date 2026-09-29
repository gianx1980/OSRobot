// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Data;
using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging.Abstract;
using OSRobot.Server.Plugins.ReadBinaryFileTask;

namespace OSRobot.Tests.TestPlugins;

[TestClass]
public class TestReadBinaryFileTask
{
    [TestMethod]
    public async Task TestRead()
    {
        // ---------
        // Arrange
        // ---------
        string basePath = AppDomain.CurrentDomain.BaseDirectory;
        string testFolder = Path.Combine(basePath, @"TestReadBinaryTask\");
        Folder folder = Common.CreateRootFolder();

        if (Directory.Exists(testFolder))
            Directory.Delete(testFolder, true);
        Directory.CreateDirectory(testFolder);
        Directory.CreateDirectory(Path.Combine(testFolder, "SubFolder"));
        Directory.CreateDirectory(Path.Combine(testFolder, "SubFolder", "SubSubFolder"));

        string[] filesToCreate =
        [
            Path.Combine(testFolder, "TestFileEnumerator1.txt"),
            Path.Combine(testFolder, "TestFileEnumerator2.txt"),
            Path.Combine(testFolder, "TestFileEnumerator3.txt"),
            Path.Combine(testFolder, "SubFolder", "TestFileEnumerator4.txt"),
            Path.Combine(testFolder, "SubFolder", "TestFileEnumerator5.txt"),
            Path.Combine(testFolder, "SubFolder", "TestFileEnumerator6.txt"),
            Path.Combine(testFolder, "SubFolder", "SubSubFolder", "TestFileEnumerator7.txt"),
            Path.Combine(testFolder, "SubFolder", "SubSubFolder", "TestFileEnumerator8.txt"),
            Path.Combine(testFolder, "SubFolder", "SubSubFolder", "TestFileEnumerator9.txt"),
        ];

        foreach (string file in filesToCreate)
        {
            Common.WriteTestFile(file, "Test content");
        }

        ReadBinaryFileTaskConfig taskConfig = new()
        {
            Id = 1,
            Name = "Read binary file task",
            FilePath = testFolder,
            Recursive = true
        };

        ReadBinaryFileTask taskRead = new()
        {
            Config = taskConfig,
            ParentFolder = folder
        };

        Common.ConfigureLogPath();
        (DynamicDataChain dynDataChain,
         DynamicDataSet dynDataSet,
         IPluginInstanceLogger logger) = Common.GetTaskDefaultParameters(taskRead);

        dynDataChain.TryAdd(2, dynDataSet);

        // ---------
        // Act
        // ---------
        taskRead.Init();
        ExecResult execResult = (await taskRead.RunAsync(dynDataChain, dynDataSet, 0, logger, CancellationToken.None)).ExecResults[0];

        DataTable dt = (DataTable)execResult.Data["DefaultRecordset"];


        // ---------
        // Assert
        // ---------
        Assert.IsTrue(filesToCreate.Length == dt.Rows.Count);

        foreach (DataRow row in dt.Rows)
        {
            byte[] fileContent = (byte[])row["FileContent"];
            string fileContentStr = System.Text.Encoding.UTF8.GetString(fileContent);
            Assert.IsTrue(fileContentStr == "Test content\r\n");
        }
    }
}
