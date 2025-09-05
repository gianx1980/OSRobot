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
using OSRobot.Server.Plugins.FileSystemTask;

namespace OSRobot.Tests.TestPlugins;


[TestClass]
public sealed class TestFileSystemTask
{

    [TestMethod]
    public void TestCopy()
    {
        // ---------
        // Arrange
        // ---------
        string basePath = AppDomain.CurrentDomain.BaseDirectory;
        string testPath = Path.Combine(basePath, @"TestFileSystemTask1\");
        string testMethodPath = Path.Combine(testPath, nameof(TestCopy));
        string testMethodDestPath = Path.Combine(testMethodPath, "Dest");
        Folder folder = Common.CreateRootFolder();

        // Prepare test base folders
        if (Directory.Exists(testPath))
            Directory.Delete(testPath, true);
        Directory.CreateDirectory(testPath);
        Directory.CreateDirectory(testMethodPath);
        Directory.CreateDirectory(testMethodDestPath);

        // Create test files
        string file1 = Path.Combine(testMethodPath, "file1.txt");
        Common.WriteTestFile(file1, "Fake content 1");

        string file2 = Path.Combine(testMethodPath, "file2.txt");
        Common.WriteTestFile(file2, "Fake content 2");

        // Prepare task
        FileSystemTaskConfig taskCopyConfig = new()
        {
            Id = 1,
            Name = "File system task (copy)",
            Command = FileSystemTaskCommandType.Copy
        };
        string file1Copy = Path.Combine(testMethodDestPath, "file1.txt");
        taskCopyConfig.CopyItems.Add
        (
            new FileSystemTaskCopyItem()
            {
                SourcePath = file1,
                DestinationPath = file1Copy,
            }
        );
        string file2Copy = Path.Combine(testMethodDestPath, "file2.txt");
        taskCopyConfig.CopyItems.Add
        (
            new FileSystemTaskCopyItem()
            {
                SourcePath = file2,
                DestinationPath = file2Copy,
            }
        );

        FileSystemTask taskCopy = new()
        {
            Config = taskCopyConfig,
            ParentFolder = folder
        };

        Common.ConfigureLogPath();
        (DynamicDataChain dynDataChain,
         DynamicDataSet dynDataSet,
         IPluginInstanceLogger logger) = Common.GetTaskDefaultParameters(taskCopy);


        // ---------
        // Act
        // ---------
        taskCopy.Init();
        ExecResult execResult = taskCopy.Run(dynDataChain, dynDataSet, 0, logger).ExecResults[0];
        taskCopy.Destroy();

        // ---------
        // Assert
        // ---------
        Assert.IsTrue(execResult.Result, "TaskCopy failed.");
        Assert.IsTrue(File.Exists(file1Copy), "File 1 copy not found");
        Assert.IsTrue(File.Exists(file2Copy), "File 2 copy not found");
    }

    [TestMethod]
    public void TestDelete()
    {
        // ---------
        // Arrange
        // ---------
        string basePath = AppDomain.CurrentDomain.BaseDirectory;
        string testPath = Path.Combine(basePath, @"TestFileSystemTask2\");
        string testMethodPath = Path.Combine(testPath, nameof(TestDelete));
        Folder folder = Common.CreateRootFolder();

        // Prepare test base folders
        if (Directory.Exists(testPath))
            Directory.Delete(testPath, true);
        Directory.CreateDirectory(testPath);
        Directory.CreateDirectory(testMethodPath);

        // Create test files
        string file1 = Path.Combine(testMethodPath, "file1.txt");
        Common.WriteTestFile(file1, "Fake content 1");

        string file2 = Path.Combine(testMethodPath, "file2.txt");
        Common.WriteTestFile(file2, "Fake content 2");

        // Prepare task
        FileSystemTaskConfig taskDeleteConfig = new()
        {
            Id = 1,
            Name = "File system task (delete)",
            Command = FileSystemTaskCommandType.Delete
        };
        taskDeleteConfig.DeleteItems.Add
        (
            new FileSystemTaskDeleteItem()
            {
                DeletePath = file1
            }
        );
        taskDeleteConfig.DeleteItems.Add
        (
            new FileSystemTaskDeleteItem()
            {
                DeletePath = file2
            }
        );

        FileSystemTask taskDelete = new()
        {
            Config = taskDeleteConfig,
            ParentFolder = folder
        };

        Common.ConfigureLogPath();
        (DynamicDataChain dynDataChain,
         DynamicDataSet dynDataSet,
         IPluginInstanceLogger logger) = Common.GetTaskDefaultParameters(taskDelete);


        // ---------
        // Act
        // ---------
        taskDelete.Init();
        ExecResult execResult = taskDelete.Run(dynDataChain, dynDataSet, 0, logger).ExecResults[0];
        taskDelete.Destroy();

        // ---------
        // Assert
        // ---------
        Assert.IsTrue(execResult.Result, "TaskDelete failed.");
        Assert.IsFalse(File.Exists(file1), "File 1 found");
        Assert.IsFalse(File.Exists(file2), "File 2 found");
    }


    [TestMethod]
    public void TestExistence()
    {
        // ---------
        // Arrange
        // ---------
        string basePath = AppDomain.CurrentDomain.BaseDirectory;
        string testPath = Path.Combine(basePath, @"TestFileSystemTask3\");
        string testMethodPath = Path.Combine(testPath, nameof(TestExistence));
        Folder folder = Common.CreateRootFolder();

        // Prepare test base folders
        if (Directory.Exists(testPath))
            Directory.Delete(testPath, true);
        Directory.CreateDirectory(testPath);
        Directory.CreateDirectory(testMethodPath);

        // Create test file
        string file1 = Path.Combine(testMethodPath, "file1.txt");
        Common.WriteTestFile(file1, "Fake content 1");

        // Prepare task
        FileSystemTaskConfig taskCheckExistenceConfig = new()
        {
            Id = 1,
            Name = "File system task (check existence)",
            Command = FileSystemTaskCommandType.CheckExistence,
            CheckExistenceFilePath = file1
        };
        
        FileSystemTask taskCheckExistence = new()
        {
            Config = taskCheckExistenceConfig,
            ParentFolder = folder
        };

        Common.ConfigureLogPath();
        (DynamicDataChain dynDataChain,
         DynamicDataSet dynDataSet,
         IPluginInstanceLogger logger) = Common.GetTaskDefaultParameters(taskCheckExistence);


        // ---------
        // Act
        // ---------
        taskCheckExistence.Init();
        ExecResult execResult = taskCheckExistence.Run(dynDataChain, dynDataSet, 0, logger).ExecResults[0];
        taskCheckExistence.Destroy();
        bool fileExists = execResult.Data["FilePathExists"] != null ? (Convert.ToInt32(execResult.Data["FilePathExists"]) == 1) : false;

        // ---------
        // Assert
        // ---------
        Assert.IsTrue(execResult.Result, "TaskExists failed.");
        Assert.IsTrue(File.Exists(file1) && fileExists, "File 1 not found");
    }

    [TestMethod]
    public void TestRename()
    {
        // ---------
        // Arrange
        // ---------
        string basePath = AppDomain.CurrentDomain.BaseDirectory;
        string testPath = Path.Combine(basePath, @"TestFileSystemTask4\");
        string testMethodPath = Path.Combine(testPath, nameof(TestRename));
        Folder folder = Common.CreateRootFolder();

        // Prepare test base folders
        if (Directory.Exists(testPath))
            Directory.Delete(testPath, true);
        Directory.CreateDirectory(testPath);
        Directory.CreateDirectory(testMethodPath);

        // Create test file
        string file1 = Path.Combine(testMethodPath, "file1.txt");
        string fileRenamed = Path.Combine(testMethodPath, "file1New.txt");
        Common.WriteTestFile(file1, "Fake content 1");

        // Prepare task
        FileSystemTaskConfig taskCheckRenameConfig = new()
        {
            Id = 1,
            Name = "File system task (rename)",
            Command = FileSystemTaskCommandType.Rename,
            RenameFromPath = file1,
            RenameToPath = fileRenamed
        };

        FileSystemTask taskRename = new()
        {
            Config = taskCheckRenameConfig,
            ParentFolder = folder
        };

        Common.ConfigureLogPath();
        (DynamicDataChain dynDataChain,
         DynamicDataSet dynDataSet,
         IPluginInstanceLogger logger) = Common.GetTaskDefaultParameters(taskRename);


        // ---------
        // Act
        // ---------
        taskRename.Init();
        ExecResult execResult = taskRename.Run(dynDataChain, dynDataSet, 0, logger).ExecResults[0];
        taskRename.Destroy();

        // ---------
        // Assert
        // ---------
        Assert.IsTrue(execResult.Result, "TaskRename failed.");
        Assert.IsTrue(File.Exists(fileRenamed), "File not renamed");
    }

    [TestMethod]
    public void TestList()
    {
        // ---------
        // Arrange
        // ---------
        string basePath = AppDomain.CurrentDomain.BaseDirectory;
        string testPath = Path.Combine(basePath, @"TestFileSystemTask5\");
        string testMethodPath = Path.Combine(testPath, nameof(TestList));
        Folder folder = Common.CreateRootFolder();

        // Prepare test base folders
        if (Directory.Exists(testPath))
            Directory.Delete(testPath, true);
        Directory.CreateDirectory(testPath);
        Directory.CreateDirectory(testMethodPath);

        // Create test files
        string file1 = Path.Combine(testMethodPath, "file1.txt");
        Common.WriteTestFile(file1, "Fake content 1");

        string file2 = Path.Combine(testMethodPath, "file2.txt");
        Common.WriteTestFile(file2, "Fake content 2");

        // Prepare task
        FileSystemTaskConfig taskCheckListConfig = new()
        {
            Id = 1,
            Name = "File system task (rename)",
            Command = FileSystemTaskCommandType.List,
            ListFolderPath = testMethodPath,
            ListFiles = true
        };

        FileSystemTask taskList = new()
        {
            Config = taskCheckListConfig,
            ParentFolder = folder
        };

        Common.ConfigureLogPath();
        (DynamicDataChain dynDataChain,
         DynamicDataSet dynDataSet,
         IPluginInstanceLogger logger) = Common.GetTaskDefaultParameters(taskList);


        // ---------
        // Act
        // ---------
        taskList.Init();
        ExecResult execResult = taskList.Run(dynDataChain, dynDataSet, 0, logger).ExecResults[0];
        taskList.Destroy();

        // ---------
        // Assert
        // ---------
        Assert.IsTrue(execResult.Result, "TaskList failed.");

        DataTable dtFiles = (DataTable)execResult.Data["DefaultRecordset"];
        Assert.IsTrue(dtFiles.Rows.Count == 2, "File count mismatch");
    }
}
