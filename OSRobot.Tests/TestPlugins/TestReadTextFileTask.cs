// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Data;
using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging.Abstract;
using OSRobot.Server.Plugins.ReadTextFileTask;

namespace OSRobot.Tests.TestPlugins;

[TestClass]
public sealed class TestReadTextFileTask
{
    [TestMethod]
    public async Task TestReadTextFile_AllRows()
    {
        // ---------
        // Arrange
        // ---------
        string basePath = AppDomain.CurrentDomain.BaseDirectory;
        string testFilePath = Path.Combine(basePath, @"TestReadTextFileTask1.txt");
        Folder folder = Common.CreateRootFolder();

        // Write test file
        string[] testFileRows = [
                @"""A"",B,C,D",
                @"""E"",F,G,H",
                @"""I"",J,K,L",
                @"""M"",N,O,P",
            ];

        File.Delete(testFilePath);

        using (StreamWriter sw = new(testFilePath))
        {
            foreach (string r in testFileRows)
            {
                sw.WriteLine(r);
            }
        }

        // ---------------
        // Act && Assert
        // ---------------

        {
            ReadTextFileTaskConfig taskConfig = new()
            {
                Id = 1,
                Name = "Read text file task",
                FilePath = testFilePath,
                ReadAllTheRowsOption = true
            };

            ReadTextFileTask taskRead = new()
            {
                Config = taskConfig,
                ParentFolder = folder
            };

            Common.ConfigureLogPath();
            (DynamicDataChain dynDataChain,
             DynamicDataSet dynDataSet,
             IPluginInstanceLogger logger) = Common.GetTaskDefaultParameters(taskRead);

            dynDataChain.TryAdd(2, dynDataSet);

            taskRead.Init();
            ExecResult execResult = (await taskRead.RunAsync(dynDataChain, dynDataSet, 0, logger, CancellationToken.None)).ExecResults[0];

            DataTable dt = (DataTable)execResult.Data["DefaultRecordset"];

            Assert.IsTrue(testFileRows.Length == dt.Rows.Count);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                Assert.IsTrue(testFileRows[i] == dt.Rows[i]["Column1"].ToString());
            }
        }

        {
            ReadTextFileTaskConfig taskConfig = new()
            {
                Id = 1,
                Name = "Read text file task",
                FilePath = testFilePath,
                ReadLastRowOption = true
            };

            ReadTextFileTask taskRead = new()
            {
                Config = taskConfig,
                ParentFolder = folder
            };

            Common.ConfigureLogPath();
            (DynamicDataChain dynDataChain,
             DynamicDataSet dynDataSet,
             IPluginInstanceLogger logger) = Common.GetTaskDefaultParameters(taskRead);

            dynDataChain.TryAdd(2, dynDataSet);

            taskRead.Init();
            ExecResult er = (await taskRead.RunAsync(dynDataChain, dynDataSet, 0, logger, CancellationToken.None)).ExecResults[0];
            taskRead.Destroy();

            DataTable dt = (DataTable)er.Data["DefaultRecordset"];

            Assert.IsTrue(dt.Rows.Count == 4);
            Assert.IsTrue(testFileRows[testFileRows.Length - 1] == dt.Rows[dt.Rows.Count - 1]["Column1"].ToString());
        }

        {
            ReadTextFileTaskConfig taskConfig = new()
            {
                Id = 1,
                Name = "Read text file task",
                FilePath = testFilePath,
                ReadAllTheRowsOption = true,
                SplitColumnsType = ReadTextFileSplitColumnsType.UseDelimiters,
                DelimiterComma = true,
                UseDoubleQuotes = true
            };

            ReadTextFileTask taskRead = new()
            {
                Config = taskConfig,
                ParentFolder = folder
            };

            Common.ConfigureLogPath();
            (DynamicDataChain dynDataChain,
             DynamicDataSet dynDataSet,
             IPluginInstanceLogger logger) = Common.GetTaskDefaultParameters(taskRead);
            
            dynDataChain.TryAdd(2, dynDataSet);

            taskRead.Init();
            ExecResult er = (await taskRead.RunAsync(dynDataChain, dynDataSet, 0, logger, CancellationToken.None)).ExecResults[0];
            taskRead.Destroy();

            DataTable dt = (DataTable)er.Data["DefaultRecordset"];

            Assert.IsTrue(dt.Rows[0]["Column1"].ToString() == "A");
            Assert.IsTrue(dt.Rows[3]["Column4"].ToString() == "P");
        }
    }

}
