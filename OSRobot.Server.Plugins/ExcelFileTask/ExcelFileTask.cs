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

public class ExcelFileTask : BaseTask
{
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

    protected override void RunTask()
    {
        if (_instanceLogger == null)
            throw new ApplicationException("There is no valid _instanceLogger instance");

        DateTime startDateTime = DateTime.Now;
        DataTable defaultRecordset = new();

        int actualIterations = 1;
        ExcelFileTaskConfig? tConfig_0 = (ExcelFileTaskConfig?)CoreHelpers.CloneObjects(Config) ?? throw new ApplicationException("Cloning configuration returned null");
        DynamicDataParser.Parse(tConfig_0, _dataChain, 0);

        try
        {
            
            switch (tConfig_0.TaskType)
            {
                case ExcelFileTaskType.AppendRow:
                case ExcelFileTaskType.InsertRow:
                    {
                        bool fileExists = File.Exists(tConfig_0.FilePath);

                        using XLWorkbook wksBook = (fileExists ? new (tConfig_0.FilePath) : new ());
                        IXLWorksheet wksSheet;
                        if (!WorksheetExists(wksBook, tConfig_0.SheetName))
                            wksSheet = wksBook.Worksheets.Add(tConfig_0.SheetName);
                        else
                            wksSheet = wksBook.Worksheet(tConfig_0.SheetName);

                        int lastRow = wksSheet.RowsUsed().Count();

                        ExcelFileTaskConfig? configCopy_0 = (ExcelFileTaskConfig?)CoreHelpers.CloneObjects(Config) ?? throw new ApplicationException("Cloning configuration returned null");
                        DynamicDataParser.Parse(configCopy_0, _dataChain, 0);

                        if (configCopy_0.AddHeaderIfEmpty && (!fileExists || lastRow == 0))
                        {
                            lastRow = 1;
                            // Create header
                            // During header creation dynamic data is parsed only one time using the data first iteration                                        
                            int colIndex = 1;
                            foreach (ExcelFileColumnDefinition col in configCopy_0.ColumnsDefinition)
                            {
                                wksSheet.Cell(lastRow, colIndex).Value = DynamicDataParser.ReplaceDynamicData(col.HeaderTitle, _dataChain, 1);
                                colIndex++;
                            }
                        }

                        if (tConfig_0.TaskType == ExcelFileTaskType.InsertRow)
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
                            DynamicDataParser.Parse(configCopy, _dataChain, i);

                            int colIndex = 1;
                            foreach (ExcelFileColumnDefinition col in configCopy.ColumnsDefinition)
                            {
                                wksSheet.Cell(lastRow, colIndex).Value = DynamicDataParser.ReplaceDynamicData(col.CellValue, _dataChain, i);
                                colIndex++;
                            }

                            lastRow++;
                            actualIterations++;
                        }

                        if (fileExists)
                            wksBook.Save();
                        else
                            wksBook.SaveAs(tConfig_0.FilePath);
                    }
                    break;

                case ExcelFileTaskType.ReadRow:
                    {
                        using XLWorkbook wksBook = new(tConfig_0.FilePath);
                        IXLWorksheet wksSheet = wksBook.Worksheet(tConfig_0.SheetName);

                        int readFromRow = 0;
                        int readToRow = 0;
                        int lastRow = wksSheet.RowsUsed().Count();
                        int lastCol;
                        if (!string.IsNullOrEmpty(tConfig_0.NumColumnsToRead))
                            lastCol = int.Parse(tConfig_0.NumColumnsToRead);
                        else
                            lastCol = wksSheet.CellsUsed().Count();

                        if (tConfig_0.ReadLastRowOption || tConfig_0.ReadRowNumberOption)
                        {
                            if (tConfig_0.ReadLastRowOption)
                            {
                                readFromRow = lastRow;
                                readToRow = lastRow;
                            }
                            else
                            {
                                readFromRow = int.Parse(tConfig_0.ReadRowNumber);
                                readToRow = readFromRow;
                            }
                        }
                        else if (tConfig_0.ReadIntervalOption)
                        {
                            switch (tConfig_0.ReadInterval)
                            {
                                case ExcelReadIntervalType.ReadLastNRows:
                                    int numberOfRows = int.Parse(tConfig_0.ReadNumberOfRows);
                                    if (lastRow - numberOfRows >= 1)
                                        readFromRow = lastRow - numberOfRows;
                                    else
                                        readFromRow = 1;

                                    readToRow = lastRow;
                                    break;

                                case ExcelReadIntervalType.ReadFromRowToRow:
                                    readFromRow = int.Parse(tConfig_0.ReadFromRow);

                                    if (readFromRow <= lastRow)
                                    {
                                        readToRow = int.Parse(tConfig_0.ReadToRow);

                                        if (readToRow > lastRow)
                                            readToRow = lastRow;
                                    }
                                    break;

                                case ExcelReadIntervalType.ReadFromRowToLastRow:
                                    readFromRow = int.Parse(tConfig_0.ReadFromRow);
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
                    break;
            }
            

            DynamicDataSet dDataSet = CommonDynamicData.BuildStandardDynamicDataSet(this, true, 0, startDateTime, DateTime.Now, _iterationsCount);

            if (tConfig_0.TaskType == ExcelFileTaskType.ReadRow)
                dDataSet.Add(CommonDynamicData.DefaultRecordsetName, defaultRecordset);
            ExecResult result = new(true, dDataSet);
            _execResults.Add(result);

            if (Config.Log)
            {
                _instanceLogger.Info(this, $"Rows processed: {_iterationsCount}");
                _instanceLogger.TaskCompleted(this);
            }
                
        }
        catch (Exception ex)
        {
            if (Config.Log)
            {
                _instanceLogger.Info(this, $"Rows processed: {actualIterations}");
                _instanceLogger.TaskError(this, ex);
            }

            DynamicDataSet dDataSet = CommonDynamicData.BuildStandardDynamicDataSet(this, false, -1, startDateTime, DateTime.Now, actualIterations);
            ExecResult result = new(false, dDataSet);
            _execResults.Add(result);
        }
    }
}
