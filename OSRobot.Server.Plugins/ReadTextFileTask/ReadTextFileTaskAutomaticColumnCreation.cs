// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Plugins.ReadTextFileTask;

class ReadTextFileTaskAutomaticColumnCreation
{
    private void FillFirstColumnsDef(string[] row, bool firstLineContainsHeader, List<ReadTextFileColumnDefinition> columnsDef)
    {
        if (firstLineContainsHeader)
        {
            for (int i = 0; i < row.Length; i++)
            {
                ReadTextFileColumnDefinition ColDef = new(row[i], ReadTextFileColumnDataType.String, string.Empty, string.Empty, true, false, i + 1, null, null);
                columnsDef.Add(ColDef);
            }
        }
        else
        {
            for (int i = 0; i < row.Length; i++)
            {
                ReadTextFileColumnDefinition ColDef = new("Column" + i.ToString(), ReadTextFileColumnDataType.String, string.Empty, string.Empty, true, false, i + 1, null, null);
                columnsDef.Add(ColDef);
            }
        }
    }

    private ReadTextFileColumnDataType DetectTypeFromString(string val)
    {
        if (int.TryParse(val, out _))
            return ReadTextFileColumnDataType.Integer;
        else if (decimal.TryParse(val, out _))
            return ReadTextFileColumnDataType.Decimal;
        else if (DateTime.TryParse(val, out _))
           return ReadTextFileColumnDataType.Datetime;
        else 
            return ReadTextFileColumnDataType.String;
    }

    /*
    public async Task<List<ReadTextFileColumnDefinition>> DetectColumnsAsync(string fileSamplePath,
                                                                            string[] delimitersArray,
                                                                            bool useDoubleQuotes,
                                                                            bool firstLineContainsHeader,
                                                                            bool treatNullStringAsNull,
                                                                            CancellationToken cancellationToken)
    {
        Task<List<ReadTextFileColumnDefinition>> T = new Task<List<ReadTextFileColumnDefinition>>(() =>
        {
            Dictionary<int, int> ColumnsDetected = new Dictionary<int, int>();
            List<ReadTextFileColumnDefinition> Result = new List<ReadTextFileColumnDefinition>();

            using (TextFieldParser FileParser = new TextFieldParser(fileSamplePath))
            {
                FileParser.TextFieldType = FieldType.Delimited;
                FileParser.Delimiters = delimitersArray;
                FileParser.HasFieldsEnclosedInQuotes = useDoubleQuotes;

                int CurrentRow = 0;
                while (!FileParser.EndOfData)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        string[]? Row = FileParser.ReadFields();
                        if (Row == null) continue;
                        
                        if (CurrentRow == 0)
                        {
                            FillFirstColumnsDef(Row, firstLineContainsHeader, Result);
                            if (firstLineContainsHeader)
                            {
                                CurrentRow++;
                                continue;
                            }        
                        }
                        
                        for (int ColIdx = 0; ColIdx < Row.Length; ColIdx++)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            if (ColumnsDetected.Keys.Contains(ColIdx))
                                continue;

                            if (treatNullStringAsNull && Row[ColIdx] == "NULL")
                                continue;

                            ReadTextFileColumnDefinition ColDef = Result[ColIdx];
                            ReadTextFileColumnDataType ColDetectedType = DetectTypeFromString(Row[ColIdx]);

                            if (ColDef.ColumnDataType != ColDetectedType)
                            {
                                if (ColDef.ColumnDataType == ReadTextFileColumnDataType.String)
                                {
                                    // If the previous type was string, change to the detected type
                                    ColDef.ColumnDataType = ColDetectedType;
                                }
                                else
                                {
                                    // Detected two types for the same column, fallback to string and
                                    // mark the column as detected.
                                    ColDef.ColumnDataType = ReadTextFileColumnDataType.String;
                                    ColumnsDetected.Add(ColIdx, ColIdx);
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch /*(MalformedLineException ex)
                    {
                        return null;
                    }

                    CurrentRow++;
                }
            }

            return Result;
        });
        T.Start();

        return await T;
    }
    */
}
