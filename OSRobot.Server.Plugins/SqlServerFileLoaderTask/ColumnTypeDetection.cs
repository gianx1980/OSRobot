// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Globalization;

namespace OSRobot.Server.Plugins.SqlServerFileLoaderTask;

public enum LoaderSqlType
{
    Bit,
    Int,
    BigInt,
    Decimal,
    Float,
    Date,
    DateTime2,
    NVarChar
}

/// <summary>A column of the destination table, as decided from the file's content.</summary>
public sealed class LoaderColumn(string name, LoaderSqlType sqlType, int precision = 0, int scale = 0, int length = 0)
{
    // Text columns are sized to the first of these that fits the longest value; beyond the last, nvarchar(max).
    private static readonly int[] _textLengths = [50, 100, 255, 500, 1000, 2000, 4000];

    public string Name { get; } = name;
    public LoaderSqlType SqlType { get; } = sqlType;
    public int Precision { get; } = precision;
    public int Scale { get; } = scale;
    /// <summary>NVarChar only: -1 means nvarchar(max).</summary>
    public int Length { get; } = length;

    public static LoaderColumn Text(string name, int maxValueLength)
    {
        int length = _textLengths.FirstOrDefault(l => l >= maxValueLength, -1);
        return new LoaderColumn(name, LoaderSqlType.NVarChar, length: length);
    }

    public string SqlDefinition => SqlType switch
    {
        LoaderSqlType.Bit => "bit",
        LoaderSqlType.Int => "int",
        LoaderSqlType.BigInt => "bigint",
        LoaderSqlType.Decimal => $"decimal({Precision}, {Scale})",
        LoaderSqlType.Float => "float",
        LoaderSqlType.Date => "date",
        LoaderSqlType.DateTime2 => "datetime2",
        _ => Length == -1 ? "nvarchar(max)" : $"nvarchar({Length})"
    };

    public Type ClrType => SqlType switch
    {
        LoaderSqlType.Bit => typeof(bool),
        LoaderSqlType.Int => typeof(int),
        LoaderSqlType.BigInt => typeof(long),
        LoaderSqlType.Decimal => typeof(decimal),
        LoaderSqlType.Float => typeof(double),
        LoaderSqlType.Date or LoaderSqlType.DateTime2 => typeof(DateTime),
        _ => typeof(string)
    };

    public override string ToString() => $"{Name} {SqlDefinition}";
}

/// <summary>
/// How cell values are recognized and converted, for one culture. The same rules serve both to detect a
/// column's type and to convert its values, so a value accepted while detecting always converts.
/// Values are strings from a text file, or typed values (double, DateTime, bool) from an Excel file.
/// </summary>
public sealed class ValueParser(CultureInfo culture)
{
    private const NumberStyles _integerStyles = NumberStyles.AllowLeadingSign;
    // No thousands separators: "1.234" or "1,234" would be ambiguous between cultures, so such values stay text.
    private const NumberStyles _decimalStyles = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;
    private const NumberStyles _floatStyles = _decimalStyles | NumberStyles.AllowExponent;

    public bool TryParseBool(object value, out bool result)
    {
        result = false;
        switch (value)
        {
            case bool b:
                result = b;
                return true;
            case string s:
                return bool.TryParse(s.Trim(), out result);
            default:
                return false;
        }
    }

    public bool TryParseLong(object value, out long result)
    {
        result = 0;
        switch (value)
        {
            case string s:
                string text = s.Trim();
                return !HasLeadingZero(text) && long.TryParse(text, _integerStyles, culture, out result);
            case double d when d == Math.Floor(d) && d >= long.MinValue && d <= long.MaxValue:
                result = (long)d;
                return true;
            default:
                return false;
        }
    }

    public bool TryParseDecimal(object value, out decimal result)
    {
        result = 0;
        switch (value)
        {
            case string s:
                string text = s.Trim();
                return !HasLeadingZero(text) && decimal.TryParse(text, _decimalStyles, culture, out result);
            case double d when double.IsFinite(d) && Math.Abs(d) < 7.9e28:
                result = (decimal)d;
                return true;
            default:
                return false;
        }
    }

    public bool TryParseDouble(object value, out double result)
    {
        result = 0;
        switch (value)
        {
            case string s:
                string text = s.Trim();
                return !HasLeadingZero(text) && double.TryParse(text, _floatStyles, culture, out result) && double.IsFinite(result);
            case double d:
                result = d;
                return true;
            default:
                return false;
        }
    }

    public bool TryParseDate(object value, out DateTime result)
    {
        result = default;
        switch (value)
        {
            case DateTime dt:
                result = dt;
                return true;
            case string s:
                string text = s.Trim();
                return LooksLikeDate(text) && DateTime.TryParse(text, culture, DateTimeStyles.AllowWhiteSpaces, out result);
            default:
                return false;
        }
    }

    /// <summary>The value as text, as it is stored in a text column.</summary>
    public string ToText(object value)
    {
        return value switch
        {
            string s => s,
            double d => d.ToString(culture),
            DateTime dt => dt.ToString(culture),
            _ => System.Convert.ToString(value, culture) ?? string.Empty
        };
    }

    /// <summary>Converts a value to the column's type. The value must have been accepted for that type while detecting it.</summary>
    public object Convert(object value, LoaderColumn column)
    {
        bool parsed;
        object result;

        switch (column.SqlType)
        {
            case LoaderSqlType.Bit:
                parsed = TryParseBool(value, out bool b);
                result = b;
                break;
            case LoaderSqlType.Int:
                parsed = TryParseLong(value, out long i) && i >= int.MinValue && i <= int.MaxValue;
                result = (int)i;
                break;
            case LoaderSqlType.BigInt:
                parsed = TryParseLong(value, out long l);
                result = l;
                break;
            case LoaderSqlType.Decimal:
                parsed = TryParseDecimal(value, out decimal m);
                result = m;
                break;
            case LoaderSqlType.Float:
                parsed = TryParseDouble(value, out double f);
                result = f;
                break;
            case LoaderSqlType.Date:
            case LoaderSqlType.DateTime2:
                parsed = TryParseDate(value, out DateTime dt);
                result = dt;
                break;
            default:
                return ToText(value);
        }

        if (!parsed)
            throw new ApplicationException($"Column '{column.Name}': value '{ToText(value)}' is not a valid {column.SqlDefinition}.");

        return result;
    }

    // "007" or "-0123" are codes, not numbers: keep their leading zeros by treating them as text.
    private static bool HasLeadingZero(string text)
    {
        string digits = text.StartsWith('-') || text.StartsWith('+') ? text[1..] : text;
        return digits.Length > 1 && digits[0] == '0' && char.IsDigit(digits[1]);
    }

    // A date has day, month and year: at least two of the same separator ("31/12/2025", "2025-12-31",
    // "31.12.2025"). Without this rule a number such as "1.5" could be read as a date or a time.
    private static bool LooksLikeDate(string text)
    {
        return text.Count(c => c == '/') >= 2 || text.Count(c => c == '-') >= 2 || text.Count(c => c == '.') >= 2;
    }
}

/// <summary>Observes the values of one column (first pass over the file) and decides its SQL type.</summary>
public sealed class ColumnProfile(ValueParser parser, bool detectType)
{
    private bool _hasValue;
    private bool _allBool = true, _allInt = true, _allLong = true, _allDecimal = true, _allDouble = true, _allDate = true;
    private bool _anyTime;
    private int _maxIntegerDigits, _maxScale, _maxTextLength;

    public void Observe(object? value)
    {
        if (value == null)
            return;

        _hasValue = true;
        _maxTextLength = Math.Max(_maxTextLength, parser.ToText(value).Length);

        if (!detectType)
            return;

        if (_allBool && !parser.TryParseBool(value, out _))
            _allBool = false;

        if (_allLong)
        {
            if (parser.TryParseLong(value, out long l))
                _allInt &= l >= int.MinValue && l <= int.MaxValue;
            else
                _allLong = _allInt = false;
        }

        if (_allDecimal)
        {
            if (parser.TryParseDecimal(value, out decimal d))
            {
                int scale = d.Scale;
                decimal integerPart = Math.Truncate(Math.Abs(d));
                int integerDigits = integerPart == 0 ? 0 : integerPart.ToString(CultureInfo.InvariantCulture).Length;
                _maxIntegerDigits = Math.Max(_maxIntegerDigits, integerDigits);
                _maxScale = Math.Max(_maxScale, scale);
            }
            else
                _allDecimal = false;
        }

        if (_allDouble && !parser.TryParseDouble(value, out _))
            _allDouble = false;

        if (_allDate)
        {
            if (parser.TryParseDate(value, out DateTime dt))
                _anyTime |= dt.TimeOfDay != TimeSpan.Zero;
            else
                _allDate = false;
        }
    }

    public LoaderColumn ToColumn(string name)
    {
        if (!detectType || !_hasValue)
            return LoaderColumn.Text(name, _maxTextLength);

        if (_allBool)
            return new LoaderColumn(name, LoaderSqlType.Bit);
        if (_allInt)
            return new LoaderColumn(name, LoaderSqlType.Int);
        if (_allLong)
            return new LoaderColumn(name, LoaderSqlType.BigInt);

        int precision = Math.Max(1, _maxIntegerDigits + _maxScale);
        if (_allDecimal && precision <= 38)
            return new LoaderColumn(name, LoaderSqlType.Decimal, precision, _maxScale);
        if (_allDouble)
            return new LoaderColumn(name, LoaderSqlType.Float);

        if (_allDate)
            return new LoaderColumn(name, _anyTime ? LoaderSqlType.DateTime2 : LoaderSqlType.Date);

        return LoaderColumn.Text(name, _maxTextLength);
    }
}

public static class LoaderColumnNames
{
    private const int _maxIdentifierLength = 128;

    /// <summary>
    /// Column names from the header row (or Column1, Column2... without one). Empty names get the default
    /// name; names repeated (SQL Server compares them case-insensitively) get a _2, _3... suffix.
    /// </summary>
    public static List<string> Build(object?[]? headerRow, int columnCount)
    {
        List<string> names = [];
        HashSet<string> used = new(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < columnCount; i++)
        {
            string? header = headerRow != null && i < headerRow.Length ? headerRow[i]?.ToString()?.Trim() : null;
            string name = string.IsNullOrEmpty(header) ? $"Column{i + 1}" : header;
            if (name.Length > _maxIdentifierLength)
                name = name[.._maxIdentifierLength];

            string unique = name;
            for (int n = 2; !used.Add(unique); n++)
            {
                string suffix = $"_{n}";
                unique = (name.Length + suffix.Length > _maxIdentifierLength ? name[..(_maxIdentifierLength - suffix.Length)] : name) + suffix;
            }

            names.Add(unique);
        }

        return names;
    }
}
