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

namespace OSRobot.Server.Core;

public abstract class IterationTask : BaseTask
{
    protected DateTime _startDateTime;
    
    #pragma warning disable CS8618
    protected ITaskConfig _iterationConfig;
    #pragma warning restore CS8618

    protected abstract void RunIteration(int currentIteration);

    protected virtual void PostIterationSucceded(int currentIteration, ExecResult result, DynamicDataSet dDataSet)
    {

    }

    protected virtual void PostIterationFailed(int currentIteration, ExecResult result, DynamicDataSet dDataSet)
    {

    }

    protected override void RunTask()
    {
        for (int i = 0; i < _iterationsCount; i++)
        {
            DateTime _startDateTime = DateTime.Now;

            try
            {
                ITaskConfig? tempTaskConfig = (ITaskConfig?)CoreHelpers.CloneObjects(Config);
                if (tempTaskConfig == null)
                    throw new ApplicationException("Cloning configuration returned null");

                _iterationConfig = tempTaskConfig;
                DynamicDataParser.Parse(_iterationConfig, _dataChain, i);

                RunIteration(i);

                DynamicDataSet dDataSet = CommonDynamicData.BuildStandardDynamicDataSet(this, true, 0, _startDateTime, DateTime.Now, _iterationsCount);
                ExecResult result = new ExecResult(true, dDataSet);

                PostIterationSucceded(i, result, dDataSet);

                _execResults.Add(result);
            }
            catch (Exception ex)
            {
                if (Config.Log)
                    _instanceLogger?.TaskError(this, ex);

                DynamicDataSet dDataSet = CommonDynamicData.BuildStandardDynamicDataSet(this, false, -1, _startDateTime, DateTime.Now, i + 1);
                ExecResult result = new ExecResult(false, dDataSet);

                PostIterationFailed(i, result, dDataSet);

                _execResults.Add(result);
            }
        }
    }
}
