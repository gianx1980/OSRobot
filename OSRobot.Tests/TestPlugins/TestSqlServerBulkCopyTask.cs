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
using OSRobot.Server.Plugins.SqlServerBulkCopyTask;
using OSRobot.Server.Plugins.SqlServerCommandTask;
using System.Data;

namespace OSRobot.Tests.TestPlugins;


[TestClass]
public class TestSqlServerBulkCopyTask
{
    [TestMethod]
    public void TestBulkCopyTable()
    {
        // ---------
        // Arrange
        // ---------

        DataTable Dt = new();
        Dt.Columns.Add("Val", typeof(int));
        
        for (int i = 1; i <= 20000; i++)
        {
            Dt.Rows.Add(i);
        }

        Folder folder = Common.CreateRootFolder();


        SqlServerCommandTaskConfig taskInitConfig = new()
        {
            Id = 10,
            Name = "Init table 1",
            Server = "(local)",
            Database = "TEST",
            Username = "Test",
            Password = "12345",
            ConnectionStringOptions = "Encrypt=no",
            Query = "DROP TABLE IF EXISTS TEST_BULKCOPY;" +
                    "CREATE TABLE TEST_BULKCOPY (VAL INT);",
            ReturnsRecordset = false,
            IterationsCount = 1,
            PluginIterationMode = IterationMode.IterateExactNumber
        };

        SqlServerCommandTask taskInit = new()
        {
            Config = taskInitConfig,
            ParentFolder = folder
        };
        
        SqlServerBulkCopyTaskConfig config = new()
        {
            Id = 1,
            Name = "Sql Server backup task 1",

            Server = "localhost",
            Database = "TEST",
            Username = "Test",
            Password = "12345",
            ConnectionStringOptions = "Encrypt=no",
            SourceRecordset = "{object[2].DefaultRecordset}",
            DestinationTable = "TEST_BULKCOPY",
            IterationsCount = 1,
            PluginIterationMode = IterationMode.IterateExactNumber
        };

        SqlServerBulkCopyTask task = new()
        {
            ParentFolder = folder,
            Config = config
        };

        Common.ConfigureLogPath();
        (DynamicDataChain dynDataChain,
         DynamicDataSet dynDataSet,
         IPluginInstanceLogger logger) = Common.GetTaskDefaultParameters(task);
        dynDataChain.TryAdd(2, dynDataSet);
        dynDataSet.TryAdd("DefaultRecordset", Dt);

        // --------------
        // Act & Assert
        // --------------
        task.Init();
        ExecResult execResultInit = taskInit.Run(dynDataChain, dynDataSet, 0, logger).ExecResults[0];
        task.Destroy();
        Assert.IsTrue(execResultInit.Result, "Task initialization failed.");

        task.Init();
        ExecResult execResult = task.Run(dynDataChain, dynDataSet, 0, logger).ExecResults[0];
        task.Destroy();

        Assert.IsTrue(execResult.Result, "Task failed.");
    }
}
