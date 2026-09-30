// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Data;
using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging.Abstract;
using OSRobot.Server.Plugins.SqlServerCommandTask;

namespace OSRobot.Tests.TestPlugins;

[TestClass]
public sealed class TestSqlServerCommandTask
{
    [TestMethod]
    public async Task TestReadWrite()
    {
        // ---------
        // Arrange
        // ---------
        string basePath = AppDomain.CurrentDomain.BaseDirectory;
        Folder folder = Common.CreateRootFolder();

        SqlServerCommandTaskConfig taskCleanConfig = new()
        {
            Id = 1,
            Name = "DB task (clean)",
            Server = "(local)",
            Database = "TEST",
            Username = "Test",
            Password = "12345",
            ConnectionStringOptions = "Encrypt=no",
            Query = "DELETE FROM TestTable",
            ReturnsRecordset = false,
            PluginIterationMode = IterationMode.IterateExactNumber,
            IterationsCount = 1
        };

        SqlServerCommandTask taskClean = new()
        {
            Config = taskCleanConfig,
            ParentFolder = folder
        };


        SqlServerCommandTaskConfig taskWriteConfig = new()
        {
            Id = 2,
            Name = "DB task (write)",
            Server = "(local)",
            Database = "TEST",
            Username = "Test",
            Password = "12345",
            ConnectionStringOptions = "Encrypt=no",
            Query = "INSERT INTO TestTable(ID, [Value]) VALUES(@ID, @VALUE)"
        };
        taskWriteConfig.ParamsDefinition.Add(
            new SqlServerParamDefinition()
            {
                Name = "@ID",
                Value = "{Object[2].DefaultRecordset['ID']}",
                Type = SqlParamType.Int,
                Length = string.Empty,
                Precision = string.Empty
            }
        );
        taskWriteConfig.ParamsDefinition.Add(
            new SqlServerParamDefinition()
            {
                Name = "@VALUE",
                Value = "{Object[2].DefaultRecordset['Value']}",
                Type = SqlParamType.Varchar,
                Length = "100",
                Precision = string.Empty
            }
        );
        taskWriteConfig.ReturnsRecordset = false;

        SqlServerCommandTask taskWrite = new()
        {
            Config = taskWriteConfig,
            ParentFolder = folder
        };

        Common.ConfigureLogPath();
        (DynamicDataChain dynDataChain,
         DynamicDataSet dynDataSet,
         IPluginInstanceLogger logger) = Common.GetTaskDefaultParameters(taskWrite);

        DataTable dt = new();
        dt.Columns.Add("ID", typeof(int));
        dt.Columns.Add("Value", typeof(string));

        DataRow dr = dt.NewRow();
        dr["ID"] = 1;
        dr["Value"] = "Matthew";
        dt.Rows.Add(dr);

        dr = dt.NewRow();
        dr["ID"] = 2;
        dr["Value"] = "Marcus";
        dt.Rows.Add(dr);

        dr = dt.NewRow();
        dr["ID"] = 3;
        dr["Value"] = "Luke";
        dt.Rows.Add(dr);

        dr = dt.NewRow();
        dr["ID"] = 4;
        dr["Value"] = "John";
        dt.Rows.Add(dr);

        dynDataSet.TryAdd("DefaultRecordset", dt);
        dynDataChain.TryAdd(2, dynDataSet);

        SqlServerCommandTaskConfig TaskReadConfig = new()
        {
            Id = 3,
            Name = "DB task (read)",
            Server = "(local)",
            Database = "TEST",
            Username = "Test",
            Password = "12345",
            ConnectionStringOptions = "Encrypt=no",
            Query = "SELECT ID, [Value] FROM TestTable ORDER BY ID",
            ReturnsRecordset = true,
            PluginIterationMode = IterationMode.IterateExactNumber,
            IterationsCount = 1
        };

        SqlServerCommandTask taskRead = new()
        {
            Config = TaskReadConfig,
            ParentFolder = folder
        };

        // ---------------
        // Act && Assert
        // ---------------
        taskClean.Init();
        taskWrite.Init();
        taskRead.Init();

        await taskClean.RunAsync(dynDataChain, dynDataSet, 0, logger, CancellationToken.None);
        await taskWrite.RunAsync(dynDataChain, dynDataSet, 0, logger, CancellationToken.None);
        ExecResult execResult = (await taskRead.RunAsync(dynDataChain, dynDataSet, 0, logger, CancellationToken.None)).ExecResults[0];

        taskClean.Destroy();
        taskWrite.Destroy();
        taskRead.Destroy();

        DataTable dtRead = (DataTable)execResult.Data["DefaultRecordset"];

        Assert.IsTrue(dt.Rows.Count == (dtRead.Rows.Count));
        Assert.IsTrue(dt.Columns.Count == dtRead.Columns.Count);

        for (int r = 0; r < dtRead.Rows.Count; r++)
        {
            for (int c = 0; c < dtRead.Columns.Count; c++)
            {
                Assert.IsTrue(dtRead.Rows[r][c].ToString() == dt.Rows[r][c].ToString());
            }
        }
    }
}
