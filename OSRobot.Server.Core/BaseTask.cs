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

public abstract class BaseTask : ITask
{
    protected object _defaultRecordset = new DataTable();
    protected int _iterationsCount;
    protected DynamicDataChain _dataChain = [];
    protected DynamicDataSet _lastDynamicDataSet = [];

    protected int? _subInstanceIndex;
#pragma warning disable CS8618
    protected IPluginInstanceLogger _instanceLogger;
    protected InstanceExecResult _instanceExecResult;
    #pragma warning restore CS8618
    
    protected List<ExecResult> _execResults = [];

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

    public InstanceExecResult Run(DynamicDataChain dataChain, DynamicDataSet lastDynamicDataSet, int? subInstanceIndex, IPluginInstanceLogger instanceLogger)
    {
        _dataChain = dataChain;
        _lastDynamicDataSet = lastDynamicDataSet;
        _subInstanceIndex = subInstanceIndex;
        _instanceLogger = instanceLogger;

        try
        {
            if (Config.Log)
                instanceLogger.TaskStarted(this);

            RunTask(dataChain, lastDynamicDataSet, subInstanceIndex, instanceLogger);

            if (Config.Log)
                instanceLogger.TaskCompleted(this);
        }
        catch (Exception ex)
        {
            if (Config.Log)
                instanceLogger.TaskError(this, ex);
        }

        if (_instanceExecResult == null)
            throw new ApplicationException("_instanceExecResult not set by the task implementation");

        return _instanceExecResult;
    }

    protected virtual void InitTask()
    {

    }

    protected virtual void DestroyTask()
    {

    }

    protected virtual void PostTaskSucceded(int currentIteration, ExecResult result, DynamicDataSet dDataSet)
    {

    }

    protected virtual void PostTaskFailed(int currentIteration, ExecResult result, DynamicDataSet dDataSet)
    {

    }

    protected abstract void RunTask(DynamicDataChain dataChain, DynamicDataSet lastDynamicDataSet, int? subInstanceIndex, IPluginInstanceLogger instanceLogger);
}
