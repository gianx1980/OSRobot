// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

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
    // Set by RunAsync() before RunTaskAsync() is invoked. Stored as a field - like _dataChain,
    // _subInstanceIndex, etc. above - rather than threaded through every abstract method
    // signature, so plugin task classes can just reference it directly in their async I/O calls.
    protected CancellationToken _cancellationToken;
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

    public async Task<InstanceExecResult> RunAsync(DynamicDataChain dataChain, DynamicDataSet lastDynamicDataSet, int? subInstanceIndex,
                                                     IPluginInstanceLogger instanceLogger, CancellationToken cancellationToken)
    {
        _dataChain = dataChain;
        _lastDynamicDataSet = lastDynamicDataSet;
        _subInstanceIndex = subInstanceIndex;
        _instanceLogger = instanceLogger;
        _cancellationToken = cancellationToken;

        try
        {
            if (Config.Log)
                instanceLogger.TaskStarted(this);

            await RunTaskAsync(dataChain, lastDynamicDataSet, subInstanceIndex, instanceLogger);

            if (Config.Log)
                instanceLogger.TaskCompleted(this);
        }
        catch (Exception ex)
        {
            if (Config.Log)
                instanceLogger.TaskError(this, ex);
        }

        _instanceExecResult = new InstanceExecResult(_execResults);

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

    protected abstract Task RunTaskAsync(DynamicDataChain dataChain, DynamicDataSet lastDynamicDataSet, int? subInstanceIndex, IPluginInstanceLogger instanceLogger);
}
