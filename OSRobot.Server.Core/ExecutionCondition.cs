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
using System.Text.Json.Serialization;

namespace OSRobot.Server.Core;

public class ExecutionCondition
{
    #pragma warning disable CS8618
    public ExecutionCondition()
    {
        
    }
    #pragma warning restore CS8618

    public ExecutionCondition(string dynamicDataCode, EnumExecutionConditionOperator conditionOperator, 
                                string minValue, string maxValue)
    {
        DynamicDataCode = dynamicDataCode;
        Operator = conditionOperator;
        MinValue = minValue;
        MaxValue = maxValue;
    }

    public string DynamicDataCode { get; set; }
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EnumExecutionConditionOperator Operator {get; set; }
    public string MinValue { get; set; }
    public string MaxValue { get; set; }

    public bool EvaluateCondition(ExecResult execResult)
    {
        if (Operator == EnumExecutionConditionOperator.ObjectExecutes && execResult.Result)
            return true;

        if (Operator == EnumExecutionConditionOperator.ObjectDoesNotExecute && !execResult.Result)
            return true;

        if (Operator == EnumExecutionConditionOperator.ValueEqualsTo && execResult.Data[DynamicDataCode].ToString() == MinValue)
            return true;

        if (Operator == EnumExecutionConditionOperator.ValueGreaterThan && (int)execResult.Data[DynamicDataCode] > int.Parse(MinValue))
            return true;

        if (Operator == EnumExecutionConditionOperator.ValueLessThan && (int)execResult.Data[DynamicDataCode] < int.Parse(MinValue))
            return true;

        if (!string.IsNullOrEmpty(DynamicDataCode))
        {
            string dynamicDataValueString = execResult.Data[DynamicDataCode].ToString() ?? string.Empty;

            if (Operator == EnumExecutionConditionOperator.ValueContains && dynamicDataValueString.Contains(MinValue))
                return true;

            if (Operator == EnumExecutionConditionOperator.ValueStartsWith && dynamicDataValueString.StartsWith(MinValue))
                return true;

            if (Operator == EnumExecutionConditionOperator.ValueEndsWith && dynamicDataValueString.EndsWith(MinValue))
                return true;

            if (Operator == EnumExecutionConditionOperator.ValueBetween
                    && (int)execResult.Data[DynamicDataCode] >= int.Parse(MinValue)
                    && (int)execResult.Data[DynamicDataCode] <= int.Parse(MaxValue)
                )
                return true;
        }

        return false;
    }
}
