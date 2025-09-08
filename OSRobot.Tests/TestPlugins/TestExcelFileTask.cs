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
using OSRobot.Server.Plugins.ExcelFileTask;
using System.Data;

namespace OSRobot.Tests.TestPlugins;

[TestClass]
public sealed class TestExcelFileTask
{
    [TestMethod]
    public void TestReadWrite()
    {
        // ---------
        // Arrange
        // ---------
        string basePath = AppDomain.CurrentDomain.BaseDirectory;
        Folder folder = Common.CreateRootFolder();

        File.Delete(Path.Combine(basePath, "TestExcel.xlsx"));

        ExcelFileTaskConfig taskWriteConfig = new()
        {
            Id = 1,
            Name = "Excel file task (write)",
            AddHeaderIfEmpty = true,
            FilePath = Path.Combine(basePath, "TestExcel.xlsx"),
            TaskType = ExcelFileTaskType.AppendRow,
            SheetName = "Test"
        };

        ExcelFileColumnDefinition column1 = new()
        {
            HeaderTitle = "ID",
            CellValue = "{Object[2].DefaultRecordset['ID']}"
        };


        ExcelFileColumnDefinition column2 = new()
        {
            HeaderTitle = "Name",
            CellValue = "{Object[2].DefaultRecordset['Value']}"
        };

        taskWriteConfig.ColumnsDefinition.Add(column1);
        taskWriteConfig.ColumnsDefinition.Add(column2);

        ExcelFileTask taskWrite = new()
        {
            Config = taskWriteConfig,
            ParentFolder = folder
        };

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


        Common.ConfigureLogPath();
        (DynamicDataChain dynDataChain,
         DynamicDataSet dynDataSet,
         IPluginInstanceLogger logger) = Common.GetTaskDefaultParameters(taskWrite);

        dynDataSet.TryAdd("DefaultRecordset", dt);
        dynDataChain.TryAdd(2, dynDataSet);

        ExcelFileTaskConfig taskReadConfig = new()
        {
            Id = 3,
            Name = "Excel file task (read)",
            AddHeaderIfEmpty = true,
            FilePath = Path.Combine(basePath, "TestExcel.xlsx"),
            TaskType = ExcelFileTaskType.ReadRow,
            SheetName = "Test",
            ReadIntervalOption = true,
            ReadLastRowOption = false,
            ReadRowNumberOption = false,
            ReadInterval = ExcelReadIntervalType.ReadFromRowToLastRow,
            ReadFromRow = "1",
            NumColumnsToRead = "2"
        };

        ExcelFileTask taskRead = new()
        {
            Config = taskReadConfig,
            ParentFolder = folder
        };

        // ---------------
        // Act && Assert
        // ---------------
        taskWrite.Init();
        taskRead.Init();

        taskWrite.Run(dynDataChain, dynDataSet, 0, logger);
        ExecResult execResult = taskRead.Run(dynDataChain, dynDataSet, 0, logger).ExecResults[0];

        taskWrite.Destroy();
        taskRead.Destroy();

        DataTable dtRead = (DataTable)execResult.Data["DefaultRecordset"];

        Assert.IsTrue(dt.Rows.Count == (dtRead.Rows.Count - 1), "Row count mismatch");    // Subtract 1 to consider header
        Assert.IsTrue(dt.Columns.Count == dtRead.Columns.Count, "Column count mismatch");
        Assert.IsTrue(((dtRead.Rows[0][0].ToString() == "ID") && (dtRead.Rows[0][1].ToString() == "Name")), "Column name mismatch");

        for (int r = 1; r < dtRead.Rows.Count; r++)
        {
            for (int c = 0; c < dtRead.Columns.Count; c++)
            {
                Assert.IsTrue(dtRead.Rows[r][c].ToString() == dt.Rows[r - 1][c].ToString());
            }
        }
    }
}
