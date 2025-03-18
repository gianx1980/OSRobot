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
using System.Data;
using System.Reflection;
using System.Text.RegularExpressions;

namespace OSRobot.Server.Core.DynamicData;

public static class DynamicDataParser
{       
    private readonly static Regex _regExFieldValue = new(@"\{object\[(?<ObjectID>\d+)\]\.(?<FieldName>\w+)(\[\'(?<SubFieldName>\w+)\'\])?\}", RegexOptions.IgnoreCase);

    public static string ReplaceDynamicData(string input, DynamicDataChain dynamicDataChain, int iterationNumber)
    {
        if (input == null)
            return string.Empty;

        return _regExFieldValue.Replace(input, (regExMatch) => {
            string result = string.Empty;
            int objectID = int.Parse(regExMatch.Groups["ObjectID"].Value);
            string fieldName = regExMatch.Groups["FieldName"].Value;
            string subFieldName;
            
            DynamicDataSet objectDataSet = dynamicDataChain[objectID];

            if (regExMatch.Groups["SubFieldName"].Value == string.Empty)
            {
                result = objectDataSet[fieldName].ToString() ?? string.Empty;
            }
            else
            {
                subFieldName = regExMatch.Groups["SubFieldName"].Value;

                if (objectDataSet[fieldName] is List<Dictionary<string, object>>)
                {
                    List<Dictionary<string, object>> list = (List<Dictionary<string, object>>)objectDataSet[fieldName];
                    Dictionary<string, object> row = list[iterationNumber];
                    result = row[subFieldName].ToString() ?? string.Empty;
                }
                else if (objectDataSet[fieldName] is DataTable list)
                {
                    DataRow row = list.Rows[iterationNumber];
                    result = row[subFieldName].ToString() ?? string.Empty;
                }
            }

            return result;
        });
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
            count = int.Parse(ReplaceDynamicData(config.IterationObject, dynamicDataChain, 0));
        }
        else // Exact number of times
        {
            count = config.IterationsCount;
        }

        return count;
    }

    public static bool ContainsDynamicData(string input)
    {
        return _regExFieldValue.IsMatch(input);
    }

    public static void Parse(IPluginInstanceConfig config, DynamicDataChain dynamicDataChain, int iterationNumber)
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
                            string result = ReplaceDynamicData(propertyValue, dynamicDataChain, iterationNumber);
                            prop.SetValue(config, result);
                        }
                        else if (prop.PropertyType == typeof(List<string>))
                        {
                            List<string>? tempList = (List<string>?)prop.GetValue(config);
                            if (tempList == null)
                                continue;
                            for (int i = 0; i <= tempList.Count - 1; i++)
                            {
                                string newT = ReplaceDynamicData(tempList[i], dynamicDataChain, iterationNumber);
                                tempList[i] = newT;
                            }
                        }
                    }
                }
            }
        }
    }

    public static void Parse(ITask task, DynamicDataChain dynamicDataChain, int iterationNumber)
    {
        IPluginInstanceConfig config = task.Config;
        Parse(config, dynamicDataChain, iterationNumber);
    }
}
