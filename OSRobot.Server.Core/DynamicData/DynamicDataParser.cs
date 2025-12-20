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
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Data;
using System.Reflection;
using System.Text.RegularExpressions;

namespace OSRobot.Server.Core.DynamicData;


public class ScriptGlobals
{
    public DynamicDataChain dynamicDataChain { get; set; } = null!;
    public int iterationNumber { get; set; }
    public int? subInstanceIndex { get; set; }
}

public class DynamicDataInfo(string dynamicData, int objectID, string fieldName, string subFieldName, string rowIndex)
{
    public string DynamicData { get; set; } = dynamicData;
    public int ObjectID { get; set; } = objectID;
    public string FieldName { get; set; } = fieldName;
    public string SubFieldName { get; set; } = subFieldName;
    public string RowIndex { get; set; } = rowIndex;
}

public static partial class DynamicDataParser
{
    private const string _codePlaceholder = "[CODE]";
    private readonly static Regex _regExFieldValue = DynamicDataRegex();
    private readonly static Regex _regExEnvVarValue = DynamicDataRegex();

    public static List<DynamicDataInfo> GetDynamicDataInfo(string input)
    {
        List<DynamicDataInfo> dynamicDataInfoList = [];

        MatchCollection matches = _regExFieldValue.Matches(input);
        foreach (Match match in matches)
        {
            dynamicDataInfoList.Add(new DynamicDataInfo(match.Value,
                                                            int.Parse(match.Groups["ObjectID"].Value),
                                                            match.Groups["FieldName"].Value,
                                                            match.Groups["SubFieldName"].Value,
                                                            match.Groups["RowIndex"].Value
                                                            ));
        }

        return dynamicDataInfoList;
    }

    public static object? GetDynamicDataValue(DynamicDataInfo dynDataInfo, DynamicDataChain dynamicDataChain, int iterationNumber, int? subInstanceIndex)
    {
        DynamicDataSet objectDataSet = dynamicDataChain[dynDataInfo.ObjectID];

        if (dynDataInfo.SubFieldName == string.Empty)
            return objectDataSet[dynDataInfo.FieldName];

        if (objectDataSet[dynDataInfo.FieldName] is List<Dictionary<string, object>> dict)
        {
            Dictionary<string, object> row = dict[GetRowIndex(dynDataInfo.RowIndex, iterationNumber, subInstanceIndex)];
            return row[dynDataInfo.SubFieldName];
        }

        if (objectDataSet[dynDataInfo.FieldName] is DataTable list)
        {
            DataRow row = list.Rows[GetRowIndex(dynDataInfo.RowIndex, iterationNumber, subInstanceIndex)];
            return row[dynDataInfo.SubFieldName];
        }

        return null;
    }

    public static string ParseBasic(string input, List<DynamicDataInfo> dynDataInfoList, DynamicDataChain dynamicDataChain, int iterationNumber, int? subInstanceIndex)
    {
        string tempParseResult = input;
        foreach (DynamicDataInfo ddi in dynDataInfoList)
        {
            string val = GetDynamicDataValue(ddi, dynamicDataChain, iterationNumber, subInstanceIndex)?.ToString() ?? string.Empty;
            tempParseResult = tempParseResult.Replace(ddi.DynamicData, val);
        }

        tempParseResult = _regExEnvVarValue.Replace(tempParseResult, (regExMatch) => {
            string varName = regExMatch.Groups["VarName"].Value;

            return Environment.GetEnvironmentVariable(varName) ?? string.Empty;
        });

        return tempParseResult;
    }

    public static string ParseCSharpCode(string input, DynamicDataChain dynamicDataChain, int iterationNumber, int? subInstanceIndex)
    {
        string code = input[_codePlaceholder.Length..];

        ScriptGlobals globals = new()
        {
            dynamicDataChain = dynamicDataChain,
            iterationNumber = iterationNumber,
            subInstanceIndex = subInstanceIndex
        };

        // OSRobot.Server.Core.DynamicData
        Assembly thisAssembly = typeof(DynamicDataSet).Assembly;
        ScriptOptions options = ScriptOptions.Default
                        .WithReferences(thisAssembly)
                        .WithImports("OSRobot.Server.Core.DynamicData");

        return CSharpScript.EvaluateAsync<string>(code, options, globals).Result;
    }

    public static int GetRowIndex(string rowIndex, int iterationNumber, int? subInstanceIndex)
    {
        if (rowIndex == string.Empty || rowIndex == "{iterationIndex}")
            return iterationNumber;
        else if (rowIndex == "{subInstanceIndex}")
            return subInstanceIndex ?? 0;
        else
            return int.Parse(rowIndex);
    }

    public static string ReplaceDynamicData(string input, DynamicDataChain dynamicDataChain, int iterationNumber, int? subInstanceIndex)
    {
        if (input == null)
            return string.Empty;

        if (input.StartsWith(_codePlaceholder))
        {
            // C# dynamic data management
            return ParseCSharpCode(input, dynamicDataChain, iterationNumber, subInstanceIndex);
        }
        else
        {
            // Basic dynamic data management

            // Given a placeholder in the format (for example):
            //  {object[3].DefaultRecordset[1]['FieldName']}
            //  Extract the parameters needed to replace the placeholder with real data
            List<DynamicDataInfo> dynamicDataInfoList = GetDynamicDataInfo(input);
            return ParseBasic(input, dynamicDataInfoList, dynamicDataChain, iterationNumber, subInstanceIndex);
        }
    }

    public static object? GetDynamicDataObject(string input, DynamicDataChain dynamicDataChain)
    {
        if (input == null)
            return null;

        object? result = null;
        Match regExMatch = _regExFieldValue.Match(input);

        if (regExMatch.Success)
        {
            int objectID = int.Parse(regExMatch.Groups["ObjectID"].Value);
            string fieldName = regExMatch.Groups["FieldName"].Value;

            DynamicDataSet objectDataSet = dynamicDataChain[objectID];
            result = objectDataSet[fieldName];
        }

        return result;
    }

    public static int GetIterationCount(ITaskConfig config, DynamicDataChain dynamicDataChain, DynamicDataSet dynamicDataSet)
    {
        int count = 1;
        
        if (config.PluginIterationMode == IterationMode.IterateDefaultRecordset)
        {
            // Look for default recordset of the previous task
            if (dynamicDataSet.ContainsKey(CommonDynamicData.DefaultRecordsetName))
            {
                if (dynamicDataSet[CommonDynamicData.DefaultRecordsetName] is List<Dictionary<string, object>> list)
                {
                    count = list.Count;
                }
                else if (dynamicDataSet[CommonDynamicData.DefaultRecordsetName] is DataTable dataTable)
                {
                    count = dataTable.Rows.Count;
                }
            }
        }
        else if (config.PluginIterationMode == IterationMode.IterateObjectRecordset)
        {
            // Look for the recordset contained in config.IterationObject
            object? source = DynamicDataParser.GetDynamicDataObject(config.IterationObject, dynamicDataChain);
            if (source != null)
            {
                if (source is List<Dictionary<string, object>> list)
                {
                    count = list.Count;
                }
                else if (source is DataTable dataTable)
                {
                    count = dataTable.Rows.Count;
                }
            }
        }
        else 
        {
            // Exact number of times
            count = config.IterationsCount;
        }

        return count;
    }

    public static bool ContainsDynamicData(string input)
    {
        return _regExFieldValue.IsMatch(input);
    }

    public static void Parse(IPluginInstanceConfig config, DynamicDataChain dynamicDataChain, int iterationNumber, int? subInstanceIndex)
    {
        Type configType = config.GetType();

        foreach (PropertyInfo prop in configType.GetProperties())
        {
            if (prop.PropertyType == typeof(string) || prop.PropertyType == typeof(List<string>))
            {
                foreach (CustomAttributeData ca in prop.CustomAttributes)
                {
                    if (ca.AttributeType == typeof(DynamicDataAttribute))
                    {
                        System.Diagnostics.Debug.WriteLine("Parsing property: " + prop.Name);

                        if (prop.PropertyType == typeof(string))
                        {
                            string? propertyValue = (string?)prop.GetValue(config);
                            if (propertyValue == null)
                                continue;
                            string result = ReplaceDynamicData(propertyValue, dynamicDataChain, iterationNumber, subInstanceIndex);
                            prop.SetValue(config, result);
                        }
                        else if (prop.PropertyType == typeof(List<string>))
                        {
                            List<string>? tempList = (List<string>?)prop.GetValue(config);
                            if (tempList == null)
                                continue;
                            for (int i = 0; i <= tempList.Count - 1; i++)
                            {
                                string newT = ReplaceDynamicData(tempList[i], dynamicDataChain, iterationNumber, subInstanceIndex);
                                tempList[i] = newT;
                            }
                        }
                    }
                }
            }
        }
    }

    public static void Parse(ITask task, DynamicDataChain dynamicDataChain, int iterationNumber, int? subInstanceIndex)
    {
        IPluginInstanceConfig config = task.Config;
        Parse(config, dynamicDataChain, iterationNumber, subInstanceIndex);
    }

    // Use Invariant Culture ("")
    [GeneratedRegex(@"\{object\[(?<ObjectID>\d+)\]\.(?<FieldName>\w+)(\[(?<RowIndex>\d+|\{iterationIndex\}|\{subInstanceIndex\})\])?(\[\'(?<SubFieldName>.*?)\'\])?\}", RegexOptions.IgnoreCase, "")]
    private static partial Regex DynamicDataRegex();

    // Use Invariant Culture ("")
    [GeneratedRegex(@"\{environment\[\'(?<VarName>.*?)\'\]\}", RegexOptions.IgnoreCase, "")]
    private static partial Regex EnvironmentRegex();
}
