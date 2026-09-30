// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Data;
using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging.Abstract;
using OSRobot.Server.Plugins.SqlServerBulkCopyTask;
using OSRobot.Server.Plugins.SqlServerCommandTask;

namespace OSRobot.Tests.TestPlugins;


[TestClass]
public class TestSqlServerBulkCopyTask
{
    [TestMethod]
    public async Task TestBulkCopyTable()
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
        ExecResult execResultInit = (await taskInit.RunAsync(dynDataChain, dynDataSet, 0, logger, CancellationToken.None)).ExecResults[0];
        task.Destroy();
        Assert.IsTrue(execResultInit.Result, "Task initialization failed.");

        task.Init();
        ExecResult execResult = (await task.RunAsync(dynDataChain, dynDataSet, 0, logger, CancellationToken.None)).ExecResults[0];
        task.Destroy();

        Assert.IsTrue(execResult.Result, "Task failed.");
    }
}
