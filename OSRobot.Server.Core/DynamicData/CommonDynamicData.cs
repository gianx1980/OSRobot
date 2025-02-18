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
namespace OSRobot.Server.Core.DynamicData;

public static class CommonDynamicData
{
    public const string DefaultRecordsetName = "DefaultRecordset";

    public const string ObjectName = "ObjectName";
    public const string ObjectID = "ObjectID";
    public const string ExecutionResult = "ExecutionResult";
    public const string ExecutionReturnValue = "ExecutionReturnValue";
    
    public const string ExecutionStartDate = "ExecutionStartDate";
    public const string ExecutionStartDateYear = "ExecutionStartDateYear";
    public const string ExecutionStartDateMonth = "ExecutionStartDateMonth";
    public const string ExecutionStartDateDay = "ExecutionStartDateDay";
    public const string ExecutionStartDateHour = "ExecutionStartDateHour";
    public const string ExecutionStartDateMinute = "ExecutionStartDateMinute";
    public const string ExecutionStartDateSecond = "ExecutionStartDateSecond";
    public const string ExecutionStartDateTicks = "ExecutionStartDateTicks";
    public const string ExecutionStartDateUnderscore = "ExecutionStartDateUnderscore";
    public const string ExecutionStartDateUnderscoreDate = "ExecutionStartDateUnderscoreDate";
    public const string ExecutionStartDateUnderscoreTime = "ExecutionStartDateUnderscoreTime";

    public const string ExecutionEndDate = "ExecutionEndDate";
    public const string ExecutionEndDateYear = "ExecutionEndDateYear";
    public const string ExecutionEndDateMonth = "ExecutionEndDateMonth";
    public const string ExecutionEndDateDay = "ExecutionEndDateDay";
    public const string ExecutionEndDateHour = "ExecutionEndDateHour";
    public const string ExecutionEndDateMinute = "ExecutionEndDateMinute";
    public const string ExecutionEndDateSecond = "ExecutionEndDateSecond";
    public const string ExecutionEndDateTicks = "ExecutionEndDateTicks";
    public const string ExecutionEndDateUnderscore = "ExecutionEndDateUnderscore";
    public const string ExecutionEndDateUnderscoreDate = "ExecutionEndDateUnderscoreDate";
    public const string ExecutionEndDateUnderscoreTime = "ExecutionEndDateUnderscoreTime";

    public const string NumberOfIterations = "NumberOfIterations";

    public static DynamicDataSet BuildStandardDynamicDataSet(IPluginInstance pluginInstance, 
                                                        bool executionResult, int executionReturnValue,
                                                        DateTime executionStartDate, DateTime executionEndDate,
                                                        int numberOfIterations)
    {
        DynamicDataSet dynDataSet = new DynamicDataSet();
        dynDataSet.Add(ObjectName, pluginInstance.Config.Name);
        dynDataSet.Add(ObjectID, pluginInstance.Config.Id);
        dynDataSet.Add(ExecutionResult, executionResult);
        dynDataSet.Add(ExecutionReturnValue, executionReturnValue);
        
        dynDataSet.Add(ExecutionStartDate, executionStartDate);
        dynDataSet.Add(ExecutionStartDateYear, executionStartDate.Year);
        dynDataSet.Add(ExecutionStartDateMonth, executionStartDate.Month);
        dynDataSet.Add(ExecutionStartDateDay, executionStartDate.Day);
        dynDataSet.Add(ExecutionStartDateHour, executionStartDate.Hour);
        dynDataSet.Add(ExecutionStartDateMinute, executionStartDate.Minute);
        dynDataSet.Add(ExecutionStartDateSecond, executionStartDate.Second);
        dynDataSet.Add(ExecutionStartDateTicks, executionStartDate.Ticks);
        dynDataSet.Add(ExecutionStartDateUnderscore, $"{executionStartDate:yyyy_MM_dd_HH_mm_ss}");
        dynDataSet.Add(ExecutionStartDateUnderscoreDate, $"{executionStartDate:yyyy_MM_dd}");
        dynDataSet.Add(ExecutionStartDateUnderscoreTime, $"{executionStartDate:HH_mm_ss}");

        dynDataSet.Add(ExecutionEndDate, executionEndDate);
        dynDataSet.Add(ExecutionEndDateYear, executionEndDate.Year);
        dynDataSet.Add(ExecutionEndDateMonth, executionEndDate.Month);
        dynDataSet.Add(ExecutionEndDateDay, executionEndDate.Day);
        dynDataSet.Add(ExecutionEndDateHour, executionEndDate.Hour);
        dynDataSet.Add(ExecutionEndDateMinute, executionEndDate.Minute);
        dynDataSet.Add(ExecutionEndDateSecond, executionEndDate.Second);
        dynDataSet.Add(ExecutionEndDateTicks, executionEndDate.Ticks);
        dynDataSet.Add(ExecutionEndDateUnderscore, $"{executionEndDate:yyyy_MM_dd_HH_mm_ss}");
        dynDataSet.Add(ExecutionEndDateUnderscoreDate, $"{executionEndDate:yyyy_MM_dd}");
        dynDataSet.Add(ExecutionEndDateUnderscoreTime, $"{executionEndDate:HH_mm_ss}");

        dynDataSet.Add(NumberOfIterations, numberOfIterations);

        return dynDataSet;
    }

    public static List<DynamicDataSample> BuildStandardDynamicDataSamples(string objectName)
    {
        return new List<DynamicDataSample>()
        {
            new DynamicDataSample(ObjectName, Resource.TxtDynDataObjectName, objectName),
            new DynamicDataSample(ObjectID, Resource.TxtDynDataObjectID, "123"),
            new DynamicDataSample(ExecutionResult, Resource.TxtDynDataExecutionResult, "1"),
            new DynamicDataSample(ExecutionReturnValue, Resource.TxtDynDataExecutionReturnValue, "1"),

            new DynamicDataSample(ExecutionStartDate, Resource.TxtDynDataExecutionStartDate, "02/20/2021 18:30:00"),
            new DynamicDataSample(ExecutionStartDateYear, Resource.TxtDynDataExecutionStartDateYear, "2021"),
            new DynamicDataSample(ExecutionStartDateMonth, Resource.TxtDynDataExecutionStartDateMonth, "2"),
            new DynamicDataSample(ExecutionStartDateDay, Resource.TxtDynDataExecutionStartDateDay, "20"),
            new DynamicDataSample(ExecutionStartDateHour, Resource.TxtDynDataExecutionStartDateHour, "18"),
            new DynamicDataSample(ExecutionStartDateMinute, Resource.TxtDynDataExecutionStartDateMinute, "30"),
            new DynamicDataSample(ExecutionStartDateSecond, Resource.TxtDynDataExecutionStartDateSecond, "0"),
            new DynamicDataSample(ExecutionStartDateTicks, Resource.TxtDynDataExecutionStartDateTicks, "3847458755"),
            new DynamicDataSample(ExecutionStartDateUnderscore, Resource.TxtDynDataExecutionStartDateUnderscore, "2021_02_20_18_30_00"),
            new DynamicDataSample(ExecutionStartDateUnderscoreDate, Resource.TxtDynDataExecutionStartDateUnderscoreDate, "2021_02_20"),
            new DynamicDataSample(ExecutionStartDateUnderscoreTime, Resource.TxtDynDataExecutionStartDateUnderscoreTime, "18_30_00"),

            new DynamicDataSample(ExecutionEndDate, Resource.TxtDynDataExecutionEndDate, "02/20/2021 18:30:00"),
            new DynamicDataSample(ExecutionEndDateYear, Resource.TxtDynDataExecutionEndDateYear, "2021"),
            new DynamicDataSample(ExecutionEndDateMonth, Resource.TxtDynDataExecutionEndDateMonth, "2"),
            new DynamicDataSample(ExecutionEndDateDay, Resource.TxtDynDataExecutionEndDateDay, "20"),
            new DynamicDataSample(ExecutionEndDateHour, Resource.TxtDynDataExecutionEndDateHour, "18"),
            new DynamicDataSample(ExecutionEndDateMinute, Resource.TxtDynDataExecutionEndDateMinute, "30"),
            new DynamicDataSample(ExecutionEndDateSecond, Resource.TxtDynDataExecutionEndDateSecond, "0"),
            new DynamicDataSample(ExecutionEndDateTicks, Resource.TxtDynDataExecutionEndDateTicks, "3847458755"),
            new DynamicDataSample(ExecutionEndDateUnderscore, Resource.TxtDynDataExecutionEndDateUnderscore, "2021_02_20_18_30_00"),
            new DynamicDataSample(ExecutionEndDateUnderscoreDate, Resource.TxtDynDataExecutionEndDateUnderscoreDate, "2021_02_20"),
            new DynamicDataSample(ExecutionEndDateUnderscoreTime, Resource.TxtDynDataExecutionEndDateUnderscoreTime, "18_30_00"),

            new DynamicDataSample(NumberOfIterations, Resource.TxtDynDataNumberOfIterations, "10")
        };
    }
}
