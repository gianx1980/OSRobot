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

using Microsoft.VisualBasic.FileIO;
using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using System.Data;
using System.Globalization;


namespace OSRobot.Server.Plugins.ReadTextFileTask;

public class ReadTextFileTask : IterationTask
{
    private string[] BuildDelimitersArray(ReadTextFileTaskConfig config)
    {
        List<string> delimiters = [];

        if (config.DelimiterTab)
            delimiters.Add("\t");
        
        if (config.DelimiterSemicolon)
            delimiters.Add(@";");

        if (config.DelimiterComma)
            delimiters.Add(@",");

        if (config.DelimiterSpace)
            delimiters.Add(@" ");

        if (config.DelimiterOther)
            delimiters.Add(config.DelimiterOtherChar);

        return [.. delimiters];
    }

    private void BuildCustomDataTable(DataTable recordset, ReadTextFileTaskConfig config)
    {
        foreach (ReadTextFileColumnDefinition colDef in config.ColumnsDefinition)
        {
            if (colDef.ColumnDataType == ReadTextFileColumnDataType.String)
                recordset.Columns.Add(colDef.ColumnName, typeof(string));
            else if (colDef.ColumnDataType == ReadTextFileColumnDataType.Integer)
                recordset.Columns.Add(colDef.ColumnName, typeof(int));
            else if (colDef.ColumnDataType == ReadTextFileColumnDataType.Decimal)
                recordset.Columns.Add(colDef.ColumnName, typeof(decimal));
            else if (colDef.ColumnDataType == ReadTextFileColumnDataType.Datetime)
                recordset.Columns.Add(colDef.ColumnName, typeof(DateTime));
        }
    }

    private void AdjustDataTableColumnsCount(DataTable dt, int columnsCount)
    {
        if (dt.Columns.Count < columnsCount)
        {
            int initialColCount = dt.Columns.Count;
            for (int i = 1; i <= columnsCount - initialColCount; i++)
            {
                dt.Columns.Add("Column" + (dt.Columns.Count + 1).ToString(), typeof(string));
            }
        }
    }

    private CultureInfo GetCulture(string culture)
    {
        if (string.IsNullOrEmpty(culture))
            return CultureInfo.InvariantCulture;

        return CultureInfo.GetCultureInfo(culture);
    }

    private string[] ReadRow(TextFieldParser parser, ReadTextFileTaskConfig config)
    {
        if (config.SplitColumnsType == ReadTextFileSplitColumnsType.None || config.SplitColumnsType == ReadTextFileSplitColumnsType.UseFixedWidthColumns)
            return [parser.ReadLine() ?? string.Empty];
        else // TextFileReadSplitColumnsType.UseDelimiters
        {
            return parser.ReadFields() ?? new string[1];
        }
    }

    private void FillDataRowColumn(ReadTextFileColumnDefinition colDef, string val, DataRow dataRow)
    {
        if (val.ToUpper().Trim() == "NULL" && colDef.ColumnTreatNullStringAsNull)
        {
            dataRow[colDef.ColumnName] = DBNull.Value;
            return;
        }               

        if (colDef.ColumnDataType == ReadTextFileColumnDataType.String)
            dataRow[colDef.ColumnName] = val;
        else if (colDef.ColumnDataType == ReadTextFileColumnDataType.Integer)
        {
            dataRow[colDef.ColumnName] = int.Parse(val, GetCulture(colDef.ColumnExpectedCulture));
        }
        else if (colDef.ColumnDataType == ReadTextFileColumnDataType.Decimal)
        {
            dataRow[colDef.ColumnName] = decimal.Parse(val, GetCulture(colDef.ColumnExpectedCulture));
        }
        else if (colDef.ColumnDataType == ReadTextFileColumnDataType.Datetime)
        {
            dataRow[colDef.ColumnName] = DateTime.ParseExact(val, colDef.ColumnExpectedFormat, GetCulture(colDef.ColumnExpectedCulture));
        }
    }

    private void AddRow(int rowIndex, string[] row, ReadTextFileTaskConfig config, DataTable recordset)
    {
        if (config.SplitColumnsType == ReadTextFileSplitColumnsType.None || config.ColumnsDefinition.Count == 0)
        {
            AdjustDataTableColumnsCount(recordset, row.Length);
            recordset.Rows.Add(row);
        }
        else if (config.SplitColumnsType == ReadTextFileSplitColumnsType.UseDelimiters)
        {
            int columnPosition = int.MinValue;

            try
            {
                DataRow dr = recordset.NewRow();
                foreach (ReadTextFileColumnDefinition ColDef in config.ColumnsDefinition)
                {
                    if (!ColDef.ColumnIsIdentity)
                    {
                        string TempColumnValue = row[(int)ColDef.ColumnPosition!];
                        FillDataRowColumn(ColDef, TempColumnValue, dr);
                    }
                }
                recordset.Rows.Add(dr);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"An error occurred adding row into datatable. Row: {rowIndex} Column position: {columnPosition}", ex);
            }
        }
        else if (config.SplitColumnsType == ReadTextFileSplitColumnsType.UseFixedWidthColumns)
        {
            int columnStartsFrom = int.MinValue;
            int columnEndsTo = int.MinValue;

            try
            {
                DataRow dr = recordset.NewRow();
                string FullRow = row[0];
                foreach (ReadTextFileColumnDefinition ColDef in config.ColumnsDefinition)
                {
                    // **** Only for logging purpose
                    if (ColDef.ColumnStartsFromCharPos == null)
                        columnStartsFrom = int.MinValue;
                    else
                        columnStartsFrom = (int)ColDef.ColumnStartsFromCharPos;

                    if (ColDef.ColumnEndsAtCharPos == null)
                        columnEndsTo = int.MinValue;
                    else
                        columnEndsTo = (int)ColDef.ColumnEndsAtCharPos;
                    // **** 

                    if (!ColDef.ColumnIsIdentity)
                    {
                        string TempColumnValue = FullRow.Substring((int)ColDef.ColumnStartsFromCharPos! - 1, (int)((ColDef.ColumnEndsAtCharPos! - 1) - (ColDef.ColumnStartsFromCharPos - 1) + 1));
                        FillDataRowColumn(ColDef, TempColumnValue, dr);
                    }
                }
                recordset.Rows.Add(dr);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"An error occurred adding row into datatable. Row: {rowIndex} Column starts from: {columnStartsFrom} Column ends to: {columnEndsTo}", ex);
            }
        }
    }

    protected override void RunIteration(int currentIteration)
    {
        // For this kind of objects consider only one iteration
        ReadTextFileTaskConfig tConfig = (ReadTextFileTaskConfig)_iterationConfig;
        DataTable defaultRecordset = new();
        _defaultRecordset = defaultRecordset;

        // If columns definition have been overriden, build the datatable according to user definition
        if (tConfig.ColumnsDefinition.Count > 0)
        {
            // Set a default column position if empty
            for (int i = 0; i < tConfig.ColumnsDefinition.Count; i++)
            {
                if (tConfig.ColumnsDefinition[i].ColumnPosition == null)
                    tConfig.ColumnsDefinition[i].ColumnPosition = i;
            }

            BuildCustomDataTable(defaultRecordset, tConfig);
        }
            

        using TextFieldParser fileParser = new(tConfig.FilePath);
        if (tConfig.SplitColumnsType == ReadTextFileSplitColumnsType.UseDelimiters)
        {
            fileParser.TextFieldType = FieldType.Delimited;
            fileParser.Delimiters = BuildDelimitersArray(tConfig);
            fileParser.HasFieldsEnclosedInQuotes = tConfig.UseDoubleQuotes;
        }

        int readFromRow = 0;
        int readToRow = 0;
        int currentRow = 1;

        if (tConfig.ReadAllTheRowsOption)
        {
            while (!fileParser.EndOfData)
            {
                try
                {
                    string[] row = ReadRow(fileParser, tConfig);

                    if (currentRow == 1 && tConfig.SkipFirstLine)
                    {
                        currentRow++;
                        continue;
                    }

                    AddRow(currentRow, row, tConfig, defaultRecordset);
                }
                catch (MalformedLineException ex)
                {
                    _instanceLogger?.Error(this, "Error parsing line", ex);
                }

                currentRow++;
            }
        }
        else if (tConfig.ReadLastRowOption)
        {
            string[]? row = null;
            while (!fileParser.EndOfData)
            {
                try
                {
                    row = ReadRow(fileParser, tConfig);
                    currentRow++;
                }
                catch (MalformedLineException ex)
                {
                    _instanceLogger?.Error(this, "Error parsing line", ex);
                }
            }
            if (row != null)
            {
                AddRow(currentRow, row, tConfig, defaultRecordset);
            }
        }
        else if (tConfig.ReadRowNumberOption || (tConfig.ReadIntervalOption && tConfig.ReadInterval == ReadTextFileIntervalType.ReadFromRowToRow))
        {
            if (tConfig.ReadRowNumberOption)
            {
                readFromRow = int.Parse(tConfig.ReadRowNumber);
                readToRow = readFromRow;
            }
            else
            {
                readFromRow = int.Parse(tConfig.ReadFromRow);
                readToRow = int.Parse(tConfig.ReadToRow);
            }

            string[]? Row = null;
            while (!fileParser.EndOfData)
            {
                try
                {
                    if (currentRow > readToRow)
                        break;

                    Row = ReadRow(fileParser, tConfig);
                    if (currentRow >= readFromRow && currentRow <= readToRow)
                    {
                        AddRow(currentRow, Row, tConfig, defaultRecordset);
                    }
                }
                catch (MalformedLineException ex)
                {
                    _instanceLogger?.Error(this, "Error parsing line", ex);
                }

                currentRow++;
            }
        }
        else
        {
            List<string[]> rows = [];
            string[]? row = null;
            while (!fileParser.EndOfData)
            {
                try
                {
                    row = ReadRow(fileParser, tConfig);
                    rows.Add(row);
                }
                catch (MalformedLineException ex)
                {
                    _instanceLogger?.Error(this, "Error parsing line", ex);
                }
            }

            if (rows.Count > 0)
            {
                if (tConfig.ReadInterval == ReadTextFileIntervalType.ReadFromRowToLastRow)
                {
                    // From row to end
                    readFromRow = int.Parse(tConfig.ReadFromRow);
                    readFromRow--;
                    if (readFromRow < 0) readFromRow = 0;
                }
                else
                {
                    // Read N Last rows
                    readFromRow = (rows.Count - 1) - int.Parse(tConfig.ReadNumberOfRows);
                }

                if (readFromRow <= (rows.Count - 1))
                {
                    for (int RowIndex = readFromRow; RowIndex < rows.Count; RowIndex++)
                    {
                        AddRow(RowIndex, rows[RowIndex], tConfig, defaultRecordset);
                    }
                }
            }
        }
    }

    private void PostIteration(int currentIteration, ExecResult result, DynamicDataSet dDataSet)
    {
        if (Config.Log)
        {
            _instanceLogger?.Info(this, $"Rows processed: {currentIteration - 1}");
            _instanceLogger?.TaskCompleted(this);
        }

        dDataSet.TryAdd(CommonDynamicData.DefaultRecordsetName, _defaultRecordset);
    }

    protected override void PostIterationSucceded(int currentIteration, ExecResult result, DynamicDataSet dDataSet)
    {
        PostIteration(currentIteration, result, dDataSet);
    }

    protected override void PostIterationFailed(int currentIteration, ExecResult result, DynamicDataSet dDataSet)
    {
        PostIteration(currentIteration, result, dDataSet);
    }
}
