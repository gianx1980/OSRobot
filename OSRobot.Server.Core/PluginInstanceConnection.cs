// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections.Generic;

namespace OSRobot.Server.Core;

public class PluginInstanceConnection
{
    #pragma warning disable CS8618
    public PluginInstanceConnection()
    {

    }
    #pragma warning restore CS8618

    public PluginInstanceConnection(IPluginInstance connectTo, bool enabled, int? waitSeconds, List<ExecutionCondition> executeConditions, List<ExecutionCondition> dontExecuteConditions)
    {
        ConnectTo = connectTo;
        Enabled = enabled;
        WaitSeconds = waitSeconds;
        ExecuteConditions = executeConditions;
        DontExecuteConditions = dontExecuteConditions;            
    }

    public IPluginInstance ConnectTo { get; set; }

    public bool Enabled { get; set; }
    public int? WaitSeconds { get; set; }
    public List<ExecutionCondition> ExecuteConditions { get; set; } = [];
    public List<ExecutionCondition> DontExecuteConditions { get; set; } = [];

    public bool EvaluateExecConditions(ExecResult execResult)
    {
        // First of all check DontExecuteCondtions
        foreach (ExecutionCondition execCond in DontExecuteConditions)
        {
            if (execCond.EvaluateCondition(execResult))
            {
                return false;
            }
        }

        // Now check ExecuteConditions
        foreach (ExecutionCondition execCond in ExecuteConditions)
        {
            if (execCond.EvaluateCondition(execResult))
            {
                return true;
            }
        }

        return false;
    }
}
