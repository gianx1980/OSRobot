// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Data;
using System.Text.Json.Serialization;
using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;

namespace OSRobot.Tests.Support;

public class TestIteratingTaskConfig : ITaskConfig
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public bool Log { get; set; } = true;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public IterationMode PluginIterationMode { get; set; } = IterationMode.IterateExactNumber;
    public string IterationObject { get; set; } = string.Empty;
    public int IterationsCount { get; set; } = 1;

    /// <summary>Each iteration is recorded in the ProbeLog as "Label#iterationIndex".</summary>
    public string Label { get; set; } = string.Empty;
    /// <summary>Rows each iteration puts in its default recordset (columns Iteration and Row); 0: no recordset.</summary>
    public int RowsPerIteration { get; set; }
    /// <summary>This iteration fails (-1: none).</summary>
    public int FailOnIteration { get; set; } = -1;
    /// <summary>Supports dynamic data, resolved per iteration, and is exposed as the "Value" output.</summary>
    [OSRobot.Server.Core.DynamicData.DynamicDataAttribute]
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// A controllable multi-iteration task for engine tests: records each iteration in <see cref="ProbeLog"/>
/// and publishes that iteration's resolved Value and its index (Iteration) as dynamic data, so each result can be told apart.
/// </summary>
public class TestIteratingTask : MultipleIterationTask
{
    protected override Task RunMultipleIterationTaskAsync(int currentIteration)
    {
        TestIteratingTaskConfig config = (TestIteratingTaskConfig)_iterationTaskConfig;
        DateTime now = DateTime.UtcNow;

        if (currentIteration == config.FailOnIteration)
            throw new InvalidOperationException($"Iteration {currentIteration} configured to fail.");

        if (config.RowsPerIteration > 0)
        {
            // Filled in place, like the real plugins do with the default recordset.
            DataTable recordset = (DataTable)_defaultRecordset;
            recordset.Columns.Add("Iteration", typeof(int));
            recordset.Columns.Add("Row", typeof(int));
            for (int row = 0; row < config.RowsPerIteration; row++)
                recordset.Rows.Add(currentIteration, row);
        }

        ProbeLog.Add(new ProbeEntry($"{config.Label}#{currentIteration}", now, now, config.Value, Cancelled: false));
        return Task.CompletedTask;
    }

    protected override void PostTaskSucceded(int currentIteration, ExecResult result, DynamicDataSet dDataSet)
    {
        dDataSet.TryAdd("Value", ((TestIteratingTaskConfig)_iterationTaskConfig).Value);
        dDataSet.TryAdd("Iteration", currentIteration);

        if (((TestIteratingTaskConfig)_iterationTaskConfig).RowsPerIteration > 0)
            dDataSet.TryAdd(CommonDynamicData.DefaultRecordsetName, _defaultRecordset);
    }
}

public class TestIteratingTaskPlugin : IPlugin
{
    public string Id => "TestIteratingTask";
    public string Title => "Test iterating task";
    public EnumPluginType PluginType => EnumPluginType.Task;
    public List<DynamicDataSample> SampleDynamicData => [.. CommonDynamicData.BuildStandardDynamicDataSamples("Iterating"), new DynamicDataSample("Value", "Value", "hello"), new DynamicDataSample("Iteration", "Iteration index", "0"),
                                                         new DynamicDataSample(CommonDynamicData.DefaultRecordsetName, "Rows", "", true)];
    public IPluginInstance GetInstance() => new TestIteratingTask();
    public IPluginInstanceConfig GetPluginDefaultConfig() => new TestIteratingTaskConfig();
    public EnumOSPlatform SupportedOSPlatforms => EnumOSPlatform.All;
}
