// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
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

        // Error handling contract, shared by SingleIterationTask and MultipleIterationTask:
        // - Plugin code signals a failure by throwing.
        // - Every failure becomes exactly one failed ExecResult for that iteration, so
        //   connections with a "failed" condition can react to it, and is always logged
        //   (Config.Log only controls the informational messages).
        // - Cancellation is not a failure: when _cancellationToken is cancelled the
        //   OperationCanceledException propagates out of RunAsync, no result is recorded
        //   and nothing downstream gets dispatched.
        DateTime executionStartDateTime = DateTime.Now;

        if (Config.Log)
            instanceLogger.TaskStarted(this);

        try
        {
            await RunTaskAsync(dataChain, lastDynamicDataSet, subInstanceIndex, instanceLogger);
        }
        catch (Exception ex) when (!IsCancellation(ex))
        {
            // Safety net: the iteration base classes already turn failures into results,
            // so this is only reached by a bug in them or in a custom RunTaskAsync.
            RecordFailure(0, executionStartDateTime, ex);
        }

        if (Config.Log)
            instanceLogger.TaskCompleted(this);

        _instanceExecResult = new InstanceExecResult(_execResults);

        return _instanceExecResult;
    }

    protected bool IsCancellation(Exception ex)
    {
        return ex is OperationCanceledException && _cancellationToken.IsCancellationRequested;
    }

    private protected virtual void LogFailure(int currentIteration, Exception ex)
    {
        _instanceLogger.TaskError(this, ex);
    }

    // Records a successful iteration. If the PostTaskSucceded hook throws, the iteration is
    // recorded as failed instead, so it never ends up with two results.
    private protected void RecordSuccess(int currentIteration, DateTime executionStartDateTime)
    {
        DynamicDataSet dDataSet = CommonDynamicData.BuildStandardDynamicDataSet(this, true, 0, executionStartDateTime, DateTime.Now, _iterationsCount);
        ExecResult result = new(true, dDataSet);

        try
        {
            PostTaskSucceded(currentIteration, result, dDataSet);
        }
        catch (Exception ex) when (!IsCancellation(ex))
        {
            RecordFailure(currentIteration, executionStartDateTime, ex);
            return;
        }

        _execResults.Add(result);
    }

    private protected void RecordFailure(int currentIteration, DateTime executionStartDateTime, Exception ex)
    {
        LogFailure(currentIteration, ex);

        DynamicDataSet dDataSet = CommonDynamicData.BuildStandardDynamicDataSet(this, false, -1, executionStartDateTime, DateTime.Now, _iterationsCount);
        ExecResult result = new(false, dDataSet);

        try
        {
            PostTaskFailed(currentIteration, result, dDataSet);
        }
        catch (Exception hookEx) when (!IsCancellation(hookEx))
        {
            _instanceLogger.Error(this, $"PostTaskFailed error (iterationIndex: {currentIteration})", hookEx);
        }

        _execResults.Add(result);
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
