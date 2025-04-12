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

using System.Text;
using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;

namespace OSRobot.Server.Plugins.WriteTextFileTask;

public class WriteTextFileTask : BaseTask
{
    private string GetDelimiter(WriteTextFileTaskConfig config)
    {
        if (config.DelimiterTab)
            return "\t";
        if (config.DelimiterSemicolon)
            return ";";
        if (config.DelimiterComma)
            return ",";
        if (config.DelimiterSpace)
            return " ";
        if (config.DelimiterOther)
            return config.DelimiterOtherChar;
        return string.Empty;
    }

    private WriteTextFileTaskConfig ParseDynamicData(int iterationNumber, WriteTextFileTaskConfig config, DynamicDataChain dataChain)
    {
        WriteTextFileTaskConfig? configCopy = (WriteTextFileTaskConfig?)CoreHelpers.CloneObjects(config) ?? throw new ApplicationException("Cloning configuration returned null");
        DynamicDataParser.Parse(configCopy, dataChain, iterationNumber, _subInstanceIndex);
        
        foreach (WriteTextFileColumnDefinition col in configCopy.ColumnsDefinition)
        {
            col.FieldValue = DynamicDataParser.ReplaceDynamicData(col.FieldValue, dataChain, iterationNumber, _subInstanceIndex);
            col.HeaderTitle = DynamicDataParser.ReplaceDynamicData(col.HeaderTitle, dataChain, iterationNumber, _subInstanceIndex);
            col.FieldWidth = DynamicDataParser.ReplaceDynamicData(col.FieldWidth, dataChain, iterationNumber, _subInstanceIndex);
        }

        return configCopy;
    }

    private bool ShouldAddHeader(WriteTextFileTaskConfig config)
    {
        if (!config.AddHeaderIfEmpty)
            return false;

        if (!File.Exists(config.FilePath))
            return true;

        FileInfo FI = new(config.FilePath);
        if (FI.Length == 0)
            return true;

        return false;
    }

    private string[] BuildHeaderArray(WriteTextFileTaskConfig config, DynamicDataChain dataChain)
    {
        List<string> fieldValues = [];

        foreach (WriteTextFileColumnDefinition col in config.ColumnsDefinition)
        {
            fieldValues.Add(col.HeaderTitle);
        }

        return [.. fieldValues];
    }


    private string[] BuildDataArray(int iterationNumber, WriteTextFileTaskConfig config, DynamicDataChain dataChain)
    {
        List<string> fieldValues = [];

        foreach (WriteTextFileColumnDefinition col in config.ColumnsDefinition)
        {
            fieldValues.Add(DynamicDataParser.ReplaceDynamicData(col.FieldValue, dataChain, iterationNumber, _subInstanceIndex));
        }

        return [.. fieldValues];
    }

    private string BuildFixedFormatString(WriteTextFileTaskConfig config)
    {
        StringBuilder sb = new();

        int colIndex = 0;
        foreach (WriteTextFileColumnDefinition col in config.ColumnsDefinition)
        {
            sb.Append($"{{{colIndex},-{col.FieldWidth}}}");

            colIndex++;
        }

        return sb.ToString();
    }

    private string BuildRow(WriteTextFileTaskConfig config, string[] fieldValues)
    {
        StringBuilder sb = new();

        if (!config.FormatAsDelimitedFile && !config.FormatAsFixedLengthColumnsFile)
        {
            // No configuration is specified, concatenate all fields
            foreach (string fieldValue in fieldValues)
            {
                sb.Append(fieldValue);
            }
        }
        else if (config.FormatAsDelimitedFile)
        {
            string delimiter = GetDelimiter(config);
            for (int c = 0; c < config.ColumnsDefinition.Count; c++)
            {
                string fieldValue = fieldValues[c];
                if (config.EncloseInDoubleQuotes)
                    fieldValue = $"\"{fieldValue.Replace("\"", "\"\"")}\"";

                sb.Append(fieldValue);

                if (c != (config.ColumnsDefinition.Count - 1))
                    sb.Append(delimiter);
            }
        }
        else if (config.FormatAsFixedLengthColumnsFile)
        {
            string fixedFormatString = BuildFixedFormatString(config);
            sb.Append(string.Format(fixedFormatString, fieldValues));
        }

        return sb.ToString();
    }

    protected override void RunTask()
    {
        WriteTextFileTaskConfig tConfig_0 = ParseDynamicData(0, (WriteTextFileTaskConfig)Config, _dataChain);
        
        int i = 0;
        bool addHeader = ShouldAddHeader(tConfig_0);
        DateTime startDateTime = DateTime.Now;

        try
        {
            switch (tConfig_0.TaskType)
            {
                case WriteTextFileTaskType.AppendRow:
                    using (StreamWriter sw = new(tConfig_0.FilePath, true))
                    {
                        for (i = 0; i < _iterationsCount; i++)
                        {
                            if (addHeader)
                            {
                                string[] headerValues = BuildHeaderArray(tConfig_0, _dataChain);
                                sw.WriteLine(BuildRow(tConfig_0, headerValues));
                                addHeader = false;
                            }

                            WriteTextFileTaskConfig configCopy = ParseDynamicData(i, (WriteTextFileTaskConfig)Config, _dataChain);
                            string[] fieldValues = BuildDataArray(i, configCopy, _dataChain);
                            sw.WriteLine(BuildRow(configCopy, fieldValues));
                        }
                    }
                    break;

                case WriteTextFileTaskType.InsertRow:
                    {
                        List<string> fileLines = [.. File.ReadAllLines(tConfig_0.FilePath)];

                        for (i = 0; i < _iterationsCount; i++)
                        {
                            if (addHeader)
                            {
                                string[] headerValues = BuildHeaderArray(tConfig_0, _dataChain);
                                fileLines.Add(BuildRow(tConfig_0, headerValues));
                                addHeader = false;
                            }

                            WriteTextFileTaskConfig configCopy = ParseDynamicData(i, (WriteTextFileTaskConfig)Config, _dataChain);
                            string[] fieldValues = BuildDataArray(i, configCopy, _dataChain);
                            fileLines.Insert(int.Parse(configCopy.InsertAtRow), BuildRow(configCopy, fieldValues));
                        }
                        File.WriteAllLines(tConfig_0.FilePath, fileLines);
                    }
                    break;

                case WriteTextFileTaskType.ReplaceText:
                    {
                        string fileContent = File.ReadAllText(tConfig_0.FilePath);
                        fileContent = fileContent.Replace(tConfig_0.FindText, tConfig_0.ReplaceWithText);
                        File.WriteAllText(tConfig_0.FilePath, fileContent);
                    }
                    break;
            }

            DynamicDataSet dDataSet = CommonDynamicData.BuildStandardDynamicDataSet(this, true, 0, startDateTime, DateTime.Now, _iterationsCount);
            ExecResult result = new(true, dDataSet);
            _execResults.Add(result);
        }
        catch (Exception ex)
        {
            if (Config.Log)
                _instanceLogger?.TaskError(this, ex);

            DynamicDataSet dDataSet = CommonDynamicData.BuildStandardDynamicDataSet(this, false, -1, startDateTime, DateTime.Now, i + 1);
            ExecResult result = new(false, dDataSet);
            _execResults.Add(result);
        }
    }
}
