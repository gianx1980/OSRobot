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

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging.Abstract;
using OSRobot.Server.Plugins.SqlServerBackupTask;

namespace OSRobot.Tests.TestPlugins;

[TestClass]
public class TestSqlServerBackupTask
{
    [TestMethod]
    public void TestBackupDB()
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
        InstanceExecResult result = task.Run(dynDataChain, dynDataSet, 0, logger);
        task.Destroy();

        // ---------
        // Assert
        // ---------
        Assert.IsTrue(result.ExecResults.Count > 0, "There are no executions.");

        ExecResult execResult = result.ExecResults[0];
        Assert.IsTrue(execResult.Result, "Task failed.");
    }
}
