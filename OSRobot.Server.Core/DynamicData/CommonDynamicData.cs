// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Data;

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

    /// <summary>
    /// Recordset with one row per iteration, each row holding that iteration's dynamic data. Present only
    /// in the data a task passes through a connection set to <see cref="EnumConnectionRunMode.OnceWithAllResults"/>.
    /// </summary>
    public const string IterationResults = "IterationResults";

    /// <summary>
    /// The dynamic data an object outputs, as shown by the editor and checked by DynamicDataValidator:
    /// what its plugin declares, plus the collected <see cref="IterationResults"/> recordset for tasks.
    /// </summary>
    public static List<DynamicDataSample> GetOutputSamples(IPlugin plugin)
    {
        // A copy: a plugin may return the same list instance every time.
        List<DynamicDataSample> samples = [.. plugin.SampleDynamicData];

        if (plugin.PluginType == EnumPluginType.Task)
            samples.Add(new DynamicDataSample(IterationResults, Resource.TxtDynDataIterationResults, Resource.TxtDynDataFieldXOfRecordsetsRow, true));

        return samples;
    }

    /// <summary>
    /// Folds all the results of one task execution (one per iteration) into the single result a
    /// <see cref="EnumConnectionRunMode.OnceWithAllResults"/> connection evaluates and passes on:
    /// - a recordset field (e.g. the default recordset) holds the rows of all the iterations, in order;
    /// - any other field holds the last iteration's value, so any reference to the task's fields still resolves;
    /// - the execution fields (result, start/end dates) describe the whole execution;
    /// - <see cref="IterationResults"/> holds each iteration's data, in order.
    /// </summary>
    /// <exception cref="ApplicationException">A column has different types in different iterations' recordsets.</exception>
    public static ExecResult BuildCollectedResult(IPluginInstance source, List<ExecResult> execResults, EnumCollectedResultRule rule)
    {
        if (execResults.Count == 0)
            throw new ArgumentException("There are no results to collect.", nameof(execResults));

        bool succeeded = rule == EnumCollectedResultRule.AllSucceeded
            ? execResults.All(r => r.Result)
            : execResults.Any(r => r.Result);

        DynamicDataSet first = execResults[0].Data;
        DynamicDataSet last = execResults[^1].Data;

        DynamicDataSet collected = [];
        foreach (KeyValuePair<string, object> field in last)
            collected[field.Key] = field.Value;

        DateTime start = first.TryGetValue(ExecutionStartDate, out object? s) && s is DateTime startDate ? startDate : DateTime.Now;
        DateTime end = last.TryGetValue(ExecutionEndDate, out object? e) && e is DateTime endDate ? endDate : DateTime.Now;
        int iterations = last.TryGetValue(NumberOfIterations, out object? n) && n is int count ? count : execResults.Count;

        foreach (KeyValuePair<string, object> field in BuildStandardDynamicDataSet(source, succeeded, succeeded ? 0 : -1, start, end, iterations))
            collected[field.Key] = field.Value;

        // Recordsets: the rows of all the iterations. An iteration without the recordset (e.g. a failed one) adds no rows.
        foreach (string field in execResults.SelectMany(r => r.Data.Keys).Distinct())
        {
            List<object> recordsets = [];
            foreach (ExecResult execResult in execResults)
            {
                if (execResult.Data.TryGetValue(field, out object? value) && value is DataTable or List<Dictionary<string, object>>)
                    recordsets.Add(value);
            }

            if (recordsets.Count > 0)
                collected[field] = UnionRecordsets(field, recordsets);
        }

        collected[IterationResults] = execResults.Select(r => new Dictionary<string, object>(r.Data)).ToList();

        return new ExecResult(succeeded, collected);
    }

    private static object UnionRecordsets(string field, List<object> recordsets)
    {
        if (recordsets.All(r => r is DataTable))
            return UnionDataTables(field, [.. recordsets.Cast<DataTable>()]);

        if (recordsets.All(r => r is List<Dictionary<string, object>>))
            return recordsets.Cast<List<Dictionary<string, object>>>().SelectMany(rows => rows).Select(row => new Dictionary<string, object>(row)).ToList();

        throw new ApplicationException($"Cannot collect the '{field}' recordsets of all the iterations: some are tables and some are lists of rows.");
    }

    // Columns are matched by name: a column missing from an iteration's table is empty (DBNull) in that
    // iteration's rows. Not DataTable.Merge: when the tables have a primary key, it combines rows with
    // equal keys instead of appending them.
    private static DataTable UnionDataTables(string field, List<DataTable> tables)
    {
        DataTable union = new(field);

        foreach (DataTable table in tables)
        {
            foreach (DataColumn column in table.Columns)
            {
                DataColumn? existing = union.Columns[column.ColumnName];
                if (existing == null)
                    union.Columns.Add(column.ColumnName, column.DataType);
                else if (existing.DataType != column.DataType)
                    throw new ApplicationException($"Cannot collect the '{field}' recordsets of all the iterations: column '{column.ColumnName}' " +
                                                   $"is {existing.DataType.Name} in one iteration and {column.DataType.Name} in another.");
            }
        }

        foreach (DataTable table in tables)
        {
            foreach (DataRow row in table.Rows)
            {
                if (row.RowState == DataRowState.Deleted)
                    continue;

                DataRow unionRow = union.NewRow();
                foreach (DataColumn column in table.Columns)
                    unionRow[column.ColumnName] = row[column];
                union.Rows.Add(unionRow);
            }
        }

        return union;
    }

    public static DynamicDataSet BuildStandardDynamicDataSet(IPluginInstance pluginInstance, 
                                                        bool executionResult, int executionReturnValue,
                                                        DateTime executionStartDate, DateTime executionEndDate,
                                                        int numberOfIterations)
    {
        DynamicDataSet dynDataSet = new();
        dynDataSet.TryAdd(ObjectName, pluginInstance.Config.Name);
        dynDataSet.TryAdd(ObjectID, pluginInstance.Config.Id);
        dynDataSet.TryAdd(ExecutionResult, executionResult);
        dynDataSet.TryAdd(ExecutionReturnValue, executionReturnValue);
        dynDataSet.TryAdd(ExecutionStartDate, executionStartDate);
        dynDataSet.TryAdd(ExecutionStartDateYear, executionStartDate.Year);
        dynDataSet.TryAdd(ExecutionStartDateMonth, executionStartDate.Month);
        dynDataSet.TryAdd(ExecutionStartDateDay, executionStartDate.Day);
        dynDataSet.TryAdd(ExecutionStartDateHour, executionStartDate.Hour);
        dynDataSet.TryAdd(ExecutionStartDateMinute, executionStartDate.Minute);
        dynDataSet.TryAdd(ExecutionStartDateSecond, executionStartDate.Second);
        dynDataSet.TryAdd(ExecutionStartDateTicks, executionStartDate.Ticks);
        dynDataSet.TryAdd(ExecutionStartDateUnderscore, $"{executionStartDate:yyyy_MM_dd_HH_mm_ss}");
        dynDataSet.TryAdd(ExecutionStartDateUnderscoreDate, $"{executionStartDate:yyyy_MM_dd}");
        dynDataSet.TryAdd(ExecutionStartDateUnderscoreTime, $"{executionStartDate:HH_mm_ss}");
        dynDataSet.TryAdd(ExecutionEndDate, executionEndDate);
        dynDataSet.TryAdd(ExecutionEndDateYear, executionEndDate.Year);
        dynDataSet.TryAdd(ExecutionEndDateMonth, executionEndDate.Month);
        dynDataSet.TryAdd(ExecutionEndDateDay, executionEndDate.Day);
        dynDataSet.TryAdd(ExecutionEndDateHour, executionEndDate.Hour);
        dynDataSet.TryAdd(ExecutionEndDateMinute, executionEndDate.Minute);
        dynDataSet.TryAdd(ExecutionEndDateSecond, executionEndDate.Second);
        dynDataSet.TryAdd(ExecutionEndDateTicks, executionEndDate.Ticks);
        dynDataSet.TryAdd(ExecutionEndDateUnderscore, $"{executionEndDate:yyyy_MM_dd_HH_mm_ss}");
        dynDataSet.TryAdd(ExecutionEndDateUnderscoreDate, $"{executionEndDate:yyyy_MM_dd}");
        dynDataSet.TryAdd(ExecutionEndDateUnderscoreTime, $"{executionEndDate:HH_mm_ss}");
        dynDataSet.TryAdd(NumberOfIterations, numberOfIterations);

        return dynDataSet;
    }

    public static List<DynamicDataSample> BuildStandardDynamicDataSamples(string objectName)
    {
        return
        [
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
        ];
    }
}
