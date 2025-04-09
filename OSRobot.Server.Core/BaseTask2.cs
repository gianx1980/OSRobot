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
using System.Data;

namespace OSRobot.Server.Core;

public abstract class BaseTask2 : ITask
{
    public IFolder? ParentFolder { get; set; }

    #pragma warning disable CS8618
    public IPluginInstanceConfig Config { get; set; }
#pragma warning restore CS8618

    public List<PluginInstanceConnection> Connections { get; set; } = [];

    public void Init()
    {
        InitTask();
    }

    public void Destroy()
    {
        DestroyTask();
    }

    protected virtual void InitTask()
    {

    }

    protected virtual void DestroyTask()
    {

    }

    protected abstract ExecResult RunTask(DynamicDataChain dataChain, DynamicDataSet lastDynamicDataSet, DataTable? inputRecordset, int? recordNumber, IPluginInstanceLogger instanceLogger);

    public ExecResult Run(DynamicDataChain dataChain, DynamicDataSet lastDynamicDataSet, DataTable? inputRecordset, int? recordNumber, IPluginInstanceLogger instanceLogger)
    {
        ExecResult execResult;

        try
        {
            if (Config.Log)
                instanceLogger.TaskStarted(this);

            execResult = RunTask(dataChain, lastDynamicDataSet, inputRecordset, recordNumber, instanceLogger);

            if (Config.Log)
                instanceLogger.TaskCompleted(this);
        }
        catch (Exception ex)
        {
            // TODO: check this step!!
            execResult = new(false, new DynamicDataSet());
            if (Config.Log)
                instanceLogger.TaskError(this, ex);
        }

        return execResult;
    }
}
