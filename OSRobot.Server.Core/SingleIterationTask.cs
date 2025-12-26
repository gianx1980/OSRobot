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

public abstract class SingleIterationTask : BaseTask
{
    #pragma warning disable CS8618
    protected ITaskConfig _taskConfig;
    #pragma warning restore CS8618

    protected abstract void RunSingleIterationTask();

    protected override void RunTask(DynamicDataChain dataChain, DynamicDataSet lastDynamicDataSet, int? subInstanceIndex, IPluginInstanceLogger instanceLogger)
    {
        DateTime executionStartDateTime = DateTime.Now; 
        _taskConfig = (ITaskConfig?)CoreHelpers.CloneObjects(Config) ?? throw new ApplicationException("Cloning configuration returned null");
        DynamicDataParser.Parse(_taskConfig, _dataChain, 0, _subInstanceIndex);
        _iterationsCount = DynamicDataParser.GetIterationCount(_taskConfig, dataChain, lastDynamicDataSet);

        if (_iterationsCount > 0)
        {
            try
            {
                RunSingleIterationTask();

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
