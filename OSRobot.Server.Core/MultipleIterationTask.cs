// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Data;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging.Abstract;

namespace OSRobot.Server.Core;

public abstract class MultipleIterationTask : BaseTask
{
    #pragma warning disable CS8618
    protected ITaskConfig _iterationTaskConfig;
    #pragma warning restore CS8618

    protected abstract Task RunMultipleIterationTaskAsync(int currentIteration);

    private protected override void LogFailure(int currentIteration, Exception ex)
    {
        _instanceLogger.TaskIterationError(this, currentIteration, ex);
    }

    protected override async Task RunTaskAsync(DynamicDataChain dataChain, DynamicDataSet lastDynamicDataSet, int? subInstanceIndex, IPluginInstanceLogger instanceLogger)
    {
        try
        {
            _iterationsCount = DynamicDataParser.GetIterationCount((ITaskConfig)Config, dataChain, lastDynamicDataSet);
        }
        catch (Exception ex) when (!IsCancellation(ex))
        {
            // Without an iteration count no iteration can run: record a single failure.
            RecordFailure(0, DateTime.Now, ex);
            return;
        }

        for (int i = 0; i < _iterationsCount; i++)
        {
            // Stop between iterations when cancelled, instead of failing every remaining one.
            _cancellationToken.ThrowIfCancellationRequested();

            DateTime executionStartDateTime = DateTime.Now;

            // Each iteration publishes its own default recordset: plugins fill this field in place, so
            // sharing it would make every iteration's result point to one table holding all the rows
            // (or fail, for plugins that add their columns at each iteration).
            _defaultRecordset = new DataTable();

            // Setup (config cloning, dynamic data parsing) is part of the iteration: a bad value
            // in one row fails that iteration only, the following ones still run.
            try
            {
                _iterationTaskConfig = (ITaskConfig?)CoreHelpers.CloneObjects(Config) ?? throw new ApplicationException("Cloning configuration returned null");
                DynamicDataParser.Parse(_iterationTaskConfig, _dataChain, i, _subInstanceIndex);

                await RunMultipleIterationTaskAsync(i);
            }
            catch (Exception ex) when (!IsCancellation(ex))
            {
                RecordFailure(i, executionStartDateTime, ex);
                continue;
            }

            RecordSuccess(i, executionStartDateTime);
        }
    }
}
