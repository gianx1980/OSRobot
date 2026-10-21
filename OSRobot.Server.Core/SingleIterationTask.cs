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

        // Setup (config cloning, dynamic data parsing) is part of the iteration: if it fails,
        // the task fails like it would if RunSingleIterationTaskAsync had thrown.
        try
        {
            _taskConfig = (ITaskConfig?)CoreHelpers.CloneObjects(Config) ?? throw new ApplicationException("Cloning configuration returned null");
            DynamicDataParser.Parse(_taskConfig, _dataChain, 0, _subInstanceIndex);
            _iterationsCount = DynamicDataParser.GetIterationCount(_taskConfig, dataChain, lastDynamicDataSet);

            if (_iterationsCount <= 0)
                return;

            await RunSingleIterationTaskAsync();
        }
        catch (Exception ex) when (!IsCancellation(ex))
        {
            RecordFailure(0, executionStartDateTime, ex);
            return;
        }

        RecordSuccess(0, executionStartDateTime);
    }
}
