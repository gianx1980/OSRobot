// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging.Abstract;
using OSRobot.Server.Plugins.SqlServerBackupTask;

namespace OSRobot.Tests.TestPlugins;

[TestClass]
public class TestSqlServerBackupTask
{
    [TestMethod]
    public async Task TestBackupDB()
    {
        // ---------
        // Arrange
        // ---------
        string backupDestFolder = "D:\\BackupTest";

        if (Directory.Exists(backupDestFolder))
            Directory.Delete(backupDestFolder, true);
        Directory.CreateDirectory(backupDestFolder);

        Folder folder = Common.CreateRootFolder();

        SqlServerBackupTaskConfig config = new()
        {
            Id = 1,
            Name = "Sql Server backup task 1",

            Server = "localhost",
            Username = "Test",
            Password = "12345",
            ConnectionStringOptions = "Encrypt=no",
            BackupType = BackupTypeEnum.Full,
            DatabasesToBackup = DatabasesToBackupEnum.AllUserDatabases,
            OverwriteIfExists = true,
            VerifyBackup = true,
            PerformChecksum = true,
            ContinueOnError = true,
            DestinationPath = backupDestFolder
        };

        SqlServerBackupTask task = new()
        {
            ParentFolder = folder,
            Config = config
        };

        Common.ConfigureLogPath();
        (DynamicDataChain dynDataChain,
         DynamicDataSet dynDataSet,
         IPluginInstanceLogger logger) = Common.GetTaskDefaultParameters(task);

        // ---------
        // Act
        // ---------
        task.Init();
        InstanceExecResult result = await task.RunAsync(dynDataChain, dynDataSet, 0, logger, CancellationToken.None);
        task.Destroy();

        // ---------
        // Assert
        // ---------
        Assert.IsTrue(result.ExecResults.Count > 0, "There are no executions.");

        ExecResult execResult = result.ExecResults[0];
        Assert.IsTrue(execResult.Result, "Task failed.");
    }
}
