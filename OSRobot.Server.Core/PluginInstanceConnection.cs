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
