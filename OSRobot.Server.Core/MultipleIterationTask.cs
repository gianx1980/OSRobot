// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging.Abstract;

namespace OSRobot.Server.Core;

public abstract class MultipleIterationTask : BaseTask
{
    #pragma warning disable CS8618
    protected ITaskConfig _iterationTaskConfig;
    #pragma warning restore CS8618

    protected abstract Task RunMultipleIterationTaskAsync(int currentIteration);

    protected override async Task RunTaskAsync(DynamicDataChain dataChain, DynamicDataSet lastDynamicDataSet, int? subInstanceIndex, IPluginInstanceLogger instanceLogger)
    {
        _iterationsCount = DynamicDataParser.GetIterationCount((ITaskConfig)Config, dataChain, lastDynamicDataSet);

        for (int i = 0; i < _iterationsCount; i++)
        {
            DateTime executionStartDateTime = DateTime.Now;
            _iterationTaskConfig = (ITaskConfig?)CoreHelpers.CloneObjects(Config) ?? throw new ApplicationException("Cloning configuration returned null");
            DynamicDataParser.Parse(_iterationTaskConfig, _dataChain, i, _subInstanceIndex);

            try
            {
                await RunMultipleIterationTaskAsync(i);

                DynamicDataSet dDataSet = CommonDynamicData.BuildStandardDynamicDataSet(this, true, 0, executionStartDateTime, DateTime.Now, _iterationsCount);
                ExecResult result = new(true, dDataSet);
                _execResults.Add(result);

                PostTaskSucceded(i, result, dDataSet);
            }
            catch (Exception ex)
            {
                if (Config.Log)
                    _instanceLogger?.TaskIterarionError(this, i, ex);

                DynamicDataSet dDataSet = CommonDynamicData.BuildStandardDynamicDataSet(this, false, -1, executionStartDateTime, DateTime.Now, _iterationsCount);
                ExecResult result = new(false, dDataSet);
                _execResults.Add(result);

                PostTaskFailed(i, result, dDataSet);
            }
        }
    }
}
