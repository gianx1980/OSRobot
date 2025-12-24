/*======================================================================================
    Copyright 2025 by Gianluca Di Bucci (gianx1980) (https://www.os-robot.com)

    This file is part of OSRobot.

    OSRobot is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    OSRobot is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with OSRobot.  If not, see <http://www.gnu.org/licenses/>.
======================================================================================*/

using System.Data;
using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging.Abstract;
using OSRobot.Server.Plugins.ReadBinaryFileTask;
using OSRobot.Server.Plugins.WriteBinaryFileTask;


namespace OSRobot.Tests.TestPlugins;

[TestClass]
public class TestWriteBinaryFileTask
{
    [TestMethod]
    public void TestWrite()
    {
        // ---------
        // Arrange
        // ---------
        string basePath = AppDomain.CurrentDomain.BaseDirectory;
        string testFolder = Path.Combine(basePath, @"TestWriteBinaryTask\");
        Folder folder = Common.CreateRootFolder();

        if (Directory.Exists(testFolder))
            Directory.Delete(testFolder, true);
        Directory.CreateDirectory(testFolder);
        Directory.CreateDirectory(Path.Combine(testFolder, "SubFolder"));
        Directory.CreateDirectory(Path.Combine(testFolder, "SubFolder", "SubSubFolder"));
        Directory.CreateDirectory(Path.Combine(testFolder, "Output"));

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

        WriteBinaryFileTaskConfig taskWriteConfig = new()
        {
            Id = 2,
            Name = "Write binary file task",
            FilePath = Path.Combine(testFolder, "Output", "{object[1].DefaultRecordset['Name']}"),
            FileContentSource = "{object[1].DefaultRecordset['FileContent']}"
        };

        WriteBinaryFileTask taskWrite = new()
        {
            Config = taskWriteConfig,
            ParentFolder = folder
        };

        Common.ConfigureLogPath();
        (DynamicDataChain dynDataChain,
         DynamicDataSet dynDataSet,
         IPluginInstanceLogger logger) = Common.GetTaskDefaultParameters(taskRead);

        // ---------
        // Act
        // ---------
        taskRead.Init();
        taskWrite.Init();
        ExecResult execResult = taskRead.Run(dynDataChain, dynDataSet, 0, logger).ExecResults[0];
        dynDataSet = execResult.Data;
        dynDataChain.TryAdd(taskRead.Config.Id, execResult.Data);
        ExecResult execResultWrite = taskWrite.Run(dynDataChain, dynDataSet, 0, logger).ExecResults[0];


        // ---------
        // Assert
        // ---------
        
        /*
        Assert.IsTrue(filesToCreate.Length == dt.Rows.Count);

        foreach (DataRow row in dt.Rows)
        {
            byte[] fileContent = (byte[])row["FileContent"];
            string fileContentStr = System.Text.Encoding.UTF8.GetString(fileContent);
            Assert.IsTrue(fileContentStr == "Test content\r\n");
        }
        */
    }
}
