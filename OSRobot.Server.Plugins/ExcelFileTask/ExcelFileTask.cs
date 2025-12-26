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

using ClosedXML.Excel;
using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using System.Data;

namespace OSRobot.Server.Plugins.ExcelFileTask;

public class ExcelFileTask : SingleIterationTask
{
    private int _actualIterations;

    private bool WorksheetExists(XLWorkbook wb, string worksheetName)
    {
        int totalWorksheets = wb.Worksheets.Count;
        for (int i = 1; i <= totalWorksheets; i++)
        {
            if (wb.Worksheet(i).Name == worksheetName)
                return true;
        }

        return false;
    }

    private void ExecTaskTypeAppendInsert(ExcelFileTaskConfig config)
    {
        bool fileExists = File.Exists(config.FilePath);

        using XLWorkbook wksBook = (fileExists ? new(config.FilePath) : new());
        IXLWorksheet wksSheet;
        if (!WorksheetExists(wksBook, config.SheetName))
            wksSheet = wksBook.Worksheets.Add(config.SheetName);
        else
            wksSheet = wksBook.Worksheet(config.SheetName);

        int lastRow = wksSheet.RowsUsed().Count();

        ExcelFileTaskConfig? configCopy_0 = (ExcelFileTaskConfig?)CoreHelpers.CloneObjects(Config) ?? throw new ApplicationException("Cloning configuration returned null");
        DynamicDataParser.Parse(configCopy_0, _dataChain, 0, _subInstanceIndex);

        if (configCopy_0.AddHeaderIfEmpty && (!fileExists || lastRow == 0))
        {
            lastRow = 1;
            // Create header
            // During header creation dynamic data is parsed only one time using the data first iteration                                        
            int colIndex = 1;
            foreach (ExcelFileColumnDefinition col in configCopy_0.ColumnsDefinition)
            {
                wksSheet.Cell(lastRow, colIndex).Value = DynamicDataParser.ReplaceDynamicData(col.HeaderTitle, _dataChain, 0, _subInstanceIndex);
                colIndex++;
            }
        }

        if (config.TaskType == ExcelFileTaskType.InsertRow)
        {
            int InsertAtRow = int.Parse(configCopy_0.InsertAtRow);
            wksSheet.Row(InsertAtRow).InsertRowsAbove(_iterationsCount);
            lastRow = InsertAtRow;
        }
        else
        {
            lastRow++;
        }

        for (int i = 0; i < _iterationsCount; i++)
        {
            ExcelFileTaskConfig? configCopy = (ExcelFileTaskConfig?)CoreHelpers.CloneObjects(Config) ?? throw new ApplicationException("Cloning configuration returned null");
            DynamicDataParser.Parse(configCopy, _dataChain, i, _subInstanceIndex);

            int colIndex = 1;
            foreach (ExcelFileColumnDefinition col in configCopy.ColumnsDefinition)
            {
                wksSheet.Cell(lastRow, colIndex).Value = DynamicDataParser.ReplaceDynamicData(col.CellValue, _dataChain, i, _subInstanceIndex);
                colIndex++;
            }

            lastRow++;
            _actualIterations++;
        }

        if (fileExists)
            wksBook.Save();
        else
            wksBook.SaveAs(config.FilePath);
    }

    private void ExecTaskTypeReadRow(ExcelFileTaskConfig config)
    {
        using XLWorkbook wksBook = new(config.FilePath);
        IXLWorksheet wksSheet;
        DataTable defaultRecordset = (DataTable)_defaultRecordset;

        wksSheet = !string.IsNullOrEmpty(config.SheetName) ? wksBook.Worksheet(config.SheetName) : wksSheet = wksBook.Worksheet(1);

        int readFromRow = 0;
        int readToRow = 0;
        int lastRow = wksSheet.RowsUsed().Count();
        int lastCol;
        if (!string.IsNullOrEmpty(config.NumColumnsToRead))
            lastCol = int.Parse(config.NumColumnsToRead);
        else
            lastCol = wksSheet.CellsUsed().Count();

        if (config.ReadLastRowOption || config.ReadRowNumberOption)
        {
            if (config.ReadLastRowOption)
            {
                readFromRow = lastRow;
                readToRow = lastRow;
            }
            else
            {
                readFromRow = int.Parse(config.ReadRowNumber);
                readToRow = readFromRow;
            }
        }
        else if (config.ReadIntervalOption)
        {
            switch (config.ReadInterval)
            {
                case ExcelReadIntervalType.ReadLastNRows:
                    int numberOfRows = int.Parse(config.ReadNumberOfRows);
                    if (lastRow - numberOfRows >= 1)
                        readFromRow = lastRow - numberOfRows;
                    else
                        readFromRow = 1;

                    readToRow = lastRow;
                    break;

                case ExcelReadIntervalType.ReadFromRowToRow:
                    readFromRow = int.Parse(config.ReadFromRow);

                    if (readFromRow <= lastRow)
                    {
                        readToRow = int.Parse(config.ReadToRow);

                        if (readToRow > lastRow)
                            readToRow = lastRow;
                    }
                    break;

                case ExcelReadIntervalType.ReadFromRowToLastRow:
                    readFromRow = int.Parse(config.ReadFromRow);
                    readToRow = lastRow;
                    break;
            }
        }

        if (readToRow >= readFromRow
            && readFromRow > 0
            && readToRow > 0)
        {
            for (int curCol = 1; curCol <= lastCol; curCol++)
            {
                defaultRecordset.Columns.Add("Column" + curCol.ToString());
            }

            for (int curRow = readFromRow; curRow <= readToRow; curRow++)
            {
                DataRow dr = defaultRecordset.NewRow();
                for (int curCol = 1; curCol <= lastCol; curCol++)
                {
                    dr[curCol - 1] = wksSheet.Cell(curRow, curCol).Value.ToString();
                }
                defaultRecordset.Rows.Add(dr);
            }
        }
    }

    protected override void RunSingleIterationTask()
    {           
        ExcelFileTaskConfig config = ((ExcelFileTaskConfig?)_taskConfig) ?? throw new ApplicationException("There is no valid _taskConfig instance");

        _actualIterations = 1;

        switch (config.TaskType)
        {
            case ExcelFileTaskType.AppendRow:
            case ExcelFileTaskType.InsertRow:
                ExecTaskTypeAppendInsert(config);
                break;

            case ExcelFileTaskType.ReadRow:
                ExecTaskTypeReadRow(config);
                break;
        }
    }

    protected override void PostTaskSucceded(int currentIteration, ExecResult result, DynamicDataSet dDataSet)
    {
        ExcelFileTaskConfig config = (ExcelFileTaskConfig)_taskConfig;

        if (config.TaskType == ExcelFileTaskType.ReadRow)
            dDataSet.TryAdd(CommonDynamicData.DefaultRecordsetName, _defaultRecordset);

        if (Config.Log)
        {
            _instanceLogger.Info(this, $"Rows processed: {_actualIterations}");
            _instanceLogger.TaskCompleted(this);
        }
    }

    protected override void PostTaskFailed(int currentIteration, ExecResult result, DynamicDataSet dDataSet)
    {
        if (Config.Log)
        {
            _instanceLogger.Info(this, $"Rows processed: {_actualIterations}");
            _instanceLogger.TaskCompleted(this);
        }
    }
}
