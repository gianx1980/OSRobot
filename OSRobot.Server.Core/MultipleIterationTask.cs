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

using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging.Abstract;

namespace OSRobot.Server.Core;

public abstract class MultipleIterationTask : BaseTask
{
    #pragma warning disable CS8618
    protected ITaskConfig _iterationTaskConfig;
    #pragma warning restore CS8618

    protected abstract void RunMultipleIterationTask(int currentIteration);

    protected override void RunTask(DynamicDataChain dataChain, DynamicDataSet lastDynamicDataSet, int? subInstanceIndex, IPluginInstanceLogger instanceLogger)
    {
        _iterationsCount = DynamicDataParser.GetIterationCount((ITaskConfig)Config, dataChain, lastDynamicDataSet);

        for (int i = 0; i < _iterationsCount; i++)
        {
            DateTime executionStartDateTime = DateTime.Now;
            _iterationTaskConfig = (ITaskConfig?)CoreHelpers.CloneObjects(Config) ?? throw new ApplicationException("Cloning configuration returned null");
            DynamicDataParser.Parse(_iterationTaskConfig, _dataChain, i, _subInstanceIndex);

            try
            {
                RunMultipleIterationTask(i);

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
