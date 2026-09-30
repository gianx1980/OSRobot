// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging.Abstract;

namespace OSRobot.Server.Core;

public abstract class SingleIterationTask : BaseTask
{
    #pragma warning disable CS8618
    protected ITaskConfig _taskConfig;
    #pragma warning restore CS8618

    protected abstract Task RunSingleIterationTaskAsync();

    protected override async Task RunTaskAsync(DynamicDataChain dataChain, DynamicDataSet lastDynamicDataSet, int? subInstanceIndex, IPluginInstanceLogger instanceLogger)
    {
        DateTime executionStartDateTime = DateTime.Now;
        _taskConfig = (ITaskConfig?)CoreHelpers.CloneObjects(Config) ?? throw new ApplicationException("Cloning configuration returned null");
        DynamicDataParser.Parse(_taskConfig, _dataChain, 0, _subInstanceIndex);
        _iterationsCount = DynamicDataParser.GetIterationCount(_taskConfig, dataChain, lastDynamicDataSet);

        if (_iterationsCount > 0)
        {
            try
            {
                await RunSingleIterationTaskAsync();

                DynamicDataSet dDataSet = CommonDynamicData.BuildStandardDynamicDataSet(this, true, 0, executionStartDateTime, DateTime.Now, _iterationsCount);
                ExecResult result = new(true, dDataSet);
                _execResults.Add(result);

                PostTaskSucceded(0, result, dDataSet);
            }
            catch
            {
                DynamicDataSet dDataSet = CommonDynamicData.BuildStandardDynamicDataSet(this, false, -1, executionStartDateTime, DateTime.Now, _iterationsCount);
                ExecResult result = new(false, dDataSet);
                _execResults.Add(result);

                PostTaskFailed(0, result, dDataSet);

                throw;
            }
        }
    }
}
