﻿/*======================================================================================
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
using System.Text.RegularExpressions;

namespace OSRobot.Server.Core.Data;

public static partial class DataValidationHelper
{
    private static readonly CultureInfo _cultureInfo = CultureInfo.InvariantCulture;

    public static bool IsLong(string value, int length, long minValue, long maxValue)
    {
        if (value.Length > length)
        {
            return false;
        }

        if (!IsLongRegex().Match(value).Success)
        {
            return false;
        }

        long num = long.Parse(value);
        if (num < minValue || num > maxValue)
        {
            return false;
        }

        return true;
    }

    public static bool IsValidDecimal(string value, int length, decimal minValue, decimal maxValue)
    {
        if (value.Length > length)
        {
            return false;
        }

        if (!decimal.TryParse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, _cultureInfo, out var result))
        {
            return false;
        }

        if (result < minValue || result > maxValue)
        {
            return false;
        }

        return true;
    }

    public static bool IsValidFloat(string value, int length, float minValue, float maxValue)
    {
        if (value.Length > length)
        {
            return false;
        }

        if (!float.TryParse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, _cultureInfo, out var result))
        {
            return false;
        }

        if (result < minValue || result > maxValue)
        {
            return false;
        }

        return true;
    }

    public static bool IsValidDouble(string value, int length, double minValue, double maxValue)
    {
        if (value.Length > length)
        {
            return false;
        }

        if (!double.TryParse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, _cultureInfo, out var result))
        {
            return false;
        }

        if (result < minValue || result > maxValue)
        {
            return false;
        }

        return true;
    }

    public static bool IsValidSqlServerDate(string value, DateTime minValue, DateTime maxValue)
    {
        bool flag = DateTime.TryParseExact(value, "yyyyMMdd", _cultureInfo, DateTimeStyles.None, out DateTime result);
        if (flag)
        {
            return true;
        }

        if (!flag)
        {
            flag = DateTime.TryParseExact(value, "yyyy-MM-dd", _cultureInfo, DateTimeStyles.None, out result);
        }

        if (flag)
        {
            return true;
        }

        if (!flag)
        {
            flag = DateTime.TryParseExact(value, "yyyy-MM-ddTHH:mm", _cultureInfo, DateTimeStyles.None, out result);
        }

        if (flag)
        {
            return true;
        }

        if (!flag)
        {
            flag = DateTime.TryParseExact(value, "yyyy-MM-ddTHH:mm:ss", _cultureInfo, DateTimeStyles.None, out result);
        }

        if (flag)
        {
            return true;
        }

        if (result < minValue || result > maxValue)
        {
            return false;
        }

        return true;
    }

    public static bool IsValidBool(string value)
    {
        return bool.TryParse(value, out _);
    }

    public static bool IsEmptyString(string value)
    {
        return string.IsNullOrEmpty(value);
    }

    public static bool IsInteger(string value, int length, int minValue, int maxValue)
    {
        if (value.Length > length)
        {
            return false;
        }

        if (!IsIntegerRegex().Match(value).Success)
        {
            return false;
        }

        int num = int.Parse(value);
        if (num < minValue || num > maxValue)
        {
            return false;
        }

        return true;
    }

    public static bool IsValidEMail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            email = Regex.Replace(email, "(@)(.+)$", DomainMapper, RegexOptions.None, TimeSpan.FromMilliseconds(200.0));
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }

        try
        {
            return Regex.IsMatch(email, "^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250.0));
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }

        static string DomainMapper(Match match)
        {
            string ascii = new IdnMapping().GetAscii(match.Groups[2].Value);
            return match.Groups[1].Value + ascii;
        }
    }

    [GeneratedRegex("^\\d+$")]
    private static partial Regex IsIntegerRegex();
    
    [GeneratedRegex("^\\d+$")]
    private static partial Regex IsLongRegex();
}
