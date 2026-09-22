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
using System.Globalization;
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

        if (Operator == EnumExecutionConditionOperator.ValueGreaterThan
                && TryGetNumber(execResult.Data[DynamicDataCode], out decimal greaterValue)
                && TryParseNumber(MinValue, out decimal greaterThan)
                && greaterValue > greaterThan)
            return true;

        if (Operator == EnumExecutionConditionOperator.ValueLessThan
                && TryGetNumber(execResult.Data[DynamicDataCode], out decimal lessValue)
                && TryParseNumber(MinValue, out decimal lessThan)
                && lessValue < lessThan)
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
                    && TryGetNumber(execResult.Data[DynamicDataCode], out decimal betweenValue)
                    && TryParseNumber(MinValue, out decimal min)
                    && TryParseNumber(MaxValue, out decimal max)
                    && betweenValue >= min
                    && betweenValue <= max
                )
                return true;
        }

        return false;
    }

    // Dynamic data holds whatever type the producing plugin used (int, long, float, double, decimal,
    // numeric text...). Compare as decimal so every numeric type works and long values stay exact.
    // A value that is not a number simply doesn't satisfy a numeric condition.
    private static bool TryGetNumber(object? value, out decimal number)
    {
        number = 0;

        switch (value)
        {
            case null:
            case bool:
            case char:
            case DateTime:
                return false;
            case string text:
                return TryParseNumber(text, out number);
            case double d:
                return double.IsFinite(d) && TryConvert(() => Convert.ToDecimal(d), out number);
            case float f:
                return float.IsFinite(f) && TryConvert(() => Convert.ToDecimal(f), out number);
            case IConvertible convertible:
                return TryConvert(() => convertible.ToDecimal(CultureInfo.InvariantCulture), out number);
            default:
                return false;
        }
    }

    private static bool TryParseNumber(string text, out decimal number) =>
        decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out number);

    private static bool TryConvert(Func<decimal> convert, out decimal number)
    {
        try
        {
            number = convert();
            return true;
        }
        catch (Exception ex) when (ex is OverflowException or InvalidCastException or FormatException)
        {
            number = 0;
            return false;
        }
    }
}
