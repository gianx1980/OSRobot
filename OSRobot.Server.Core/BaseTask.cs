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
    protected IPluginInstanceLogger? _instanceLogger;
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

    protected virtual void InitTask()
    {

    }

    protected virtual void DestroyTask()
    {

    }

    protected abstract void RunTask();

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

            _iterationsCount = DynamicDataParser.GetIterationCount((ITaskConfig)Config, dataChain, lastDynamicDataSet);

            if (_iterationsCount > 0)
                RunTask();

            if (Config.Log)
                instanceLogger.TaskCompleted(this);
        }
        catch (Exception ex)
        {
            if (Config.Log)
                instanceLogger.TaskError(this, ex);
        }

        return new InstanceExecResult(_execResults);
    }
}
