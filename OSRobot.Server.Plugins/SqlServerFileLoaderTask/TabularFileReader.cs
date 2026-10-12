// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using ClosedXML.Excel;
using Microsoft.VisualBasic.FileIO;
using System.Text;

namespace OSRobot.Server.Plugins.SqlServerFileLoaderTask;

/// <summary>
/// Reads the rows of a delimited text file or of an Excel (.xlsx) sheet, as raw cell values: strings for a
/// text file; for Excel the cell's own type (string, double, DateTime, bool). Empty cells are null.
/// The rows can be enumerated more than once: a text file is read again each time, so memory stays bounded.
/// </summary>
public sealed class TabularFileReader
{
    // Separators recognized in a text file, in order of preference when two fit equally well.
    private static readonly char[] _candidateSeparators = [';', ',', '\t', '|'];
    private const int _separatorSampleLines = 50;
    private const int _encodingSampleBytes = 1024 * 1024;

    private readonly string _filePath;
    private readonly string _sheetName;
    private List<object?[]>? _excelRows;

    static TabularFileReader()
    {
        // Windows-1252, for text files that are not UTF-8.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <param name="separator">Text files only: the column separator as written in the configuration (see
    /// <see cref="ParseSeparator"/>); null to detect it. Not used, nor checked, for an Excel file.</param>
    /// <param name="encoding">Text files only: the encoding name from the configuration (see <see cref="ParseEncoding"/>);
    /// null to detect it. A byte order mark in the file wins either way. Not used, nor checked, for an Excel file.</param>
    public TabularFileReader(string filePath, string sheetName, string? separator = null, string? encoding = null)
    {
        _filePath = filePath;
        _sheetName = sheetName;

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File '{filePath}' does not exist.", filePath);

        string extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (extension == ".xls")
            throw new ApplicationException($"File '{filePath}': the old Excel format (.xls) is not supported, save it as .xlsx.");

        IsExcel = extension is ".xlsx" or ".xlsm";

        if (!IsExcel)
        {
            // The configured encoding is checked even when a byte order mark overrides it, so a wrong
            // setting shows up right away instead of only with the files that have no byte order mark.
            Encoding? configured = encoding != null ? ParseEncoding(encoding) : null;
            Encoding? fromByteOrderMark = ReadByteOrderMark(filePath);
            Encoding = fromByteOrderMark ?? configured ?? DetectEncoding(filePath);
            EncodingDetected = configured == null || fromByteOrderMark != null;

            SeparatorDetected = separator == null;
            Separator = separator != null ? ParseSeparator(separator) : DetectSeparator(ReadSampleLines(filePath, Encoding))?.ToString();
        }
    }

    public bool IsExcel { get; }

    /// <summary>Text files only.</summary>
    public Encoding? Encoding { get; }

    /// <summary>Text files only: whether <see cref="Encoding"/> came from the file (detected, or its byte order mark) rather than from the configuration.</summary>
    public bool EncodingDetected { get; }

    /// <summary>An encoding by name, e.g. "utf-8", "windows-1252", "iso-8859-15".</summary>
    public static Encoding ParseEncoding(string encoding)
    {
        if (string.IsNullOrWhiteSpace(encoding))
            throw new ApplicationException("The encoding is empty: choose it, or turn on Autodetect encoding.");

        try
        {
            return Encoding.GetEncoding(encoding.Trim());
        }
        catch (ArgumentException ex)
        {
            throw new ApplicationException($"'{encoding}' is not a known encoding: use a name such as utf-8 or windows-1252.", ex);
        }
    }

    /// <summary>Text files only: the column separator, or null if the file has a single column.</summary>
    public string? Separator { get; }

    /// <summary>Text files only: whether <see cref="Separator"/> was detected from the file rather than given.</summary>
    public bool SeparatorDetected { get; }

    /// <summary>
    /// A separator as the user writes it in the configuration: any text, with "\t" standing for a tab
    /// (a tab can't be typed in a text box).
    /// </summary>
    public static string ParseSeparator(string separator)
    {
        if (string.IsNullOrEmpty(separator))
            throw new ApplicationException("The separator is empty: enter it, or turn on Autodetect separator.");

        return separator.Replace("\\t", "\t");
    }

    public IEnumerable<object?[]> ReadRows() => IsExcel ? ReadExcelRows() : ReadTextRows();

    private IEnumerable<object?[]> ReadTextRows()
    {
        using TextFieldParser parser = new(_filePath, Encoding!);
        parser.TextFieldType = FieldType.Delimited;
        if (Separator != null)
            parser.SetDelimiters(Separator);
        parser.HasFieldsEnclosedInQuotes = Separator != null;
        parser.TrimWhiteSpace = false;

        while (!parser.EndOfData)
        {
            string?[]? fields;
            try
            {
                if (Separator == null)
                    fields = [parser.ReadLine()];
                else
                    fields = parser.ReadFields();
            }
            catch (MalformedLineException ex)
            {
                throw new ApplicationException($"File '{_filePath}': line {ex.LineNumber} is malformed (check its double quotes).", ex);
            }

            object?[] values = [.. (fields ?? []).Select(f => string.IsNullOrWhiteSpace(f) ? null : (object)f)];

            // Blank lines are skipped.
            if (values.Any(v => v != null))
                yield return values;
        }
    }

    private IEnumerable<object?[]> ReadExcelRows()
    {
        // ClosedXML loads the whole workbook anyway: read it once and keep the rows.
        _excelRows ??= LoadExcelRows();
        return _excelRows;
    }

    private List<object?[]> LoadExcelRows()
    {
        using XLWorkbook workbook = new(_filePath);

        IXLWorksheet worksheet;
        if (string.IsNullOrEmpty(_sheetName))
            worksheet = workbook.Worksheet(1);
        else if (!workbook.TryGetWorksheet(_sheetName, out worksheet))
            throw new ApplicationException($"File '{_filePath}': sheet '{_sheetName}' does not exist.");

        List<object?[]> rows = [];
        IXLRange? range = worksheet.RangeUsed();
        if (range == null)
            return rows;

        int columnCount = range.ColumnCount();
        foreach (IXLRangeRow row in range.Rows())
        {
            object?[] values = new object?[columnCount];
            for (int c = 0; c < columnCount; c++)
                values[c] = ToValue(row.Cell(c + 1).Value);

            // Rows left empty inside the used range (e.g. a cleared row) are skipped, as in a text file.
            if (values.Any(v => v != null))
                rows.Add(values);
        }

        return rows;
    }

    private static object? ToValue(XLCellValue value)
    {
        return value.Type switch
        {
            XLDataType.Blank => null,
            XLDataType.Boolean => value.GetBoolean(),
            XLDataType.Number => value.GetNumber(),
            XLDataType.DateTime => value.GetDateTime(),
            XLDataType.TimeSpan => value.GetTimeSpan().ToString(),
            XLDataType.Error => value.GetError().ToString(),
            _ => string.IsNullOrWhiteSpace(value.GetText()) ? null : value.GetText()
        };
    }

    // A byte order mark decides. Otherwise UTF-8 if the start of the file is valid UTF-8, else Windows-1252
    // (the usual encoding of text files exported by Windows programs in Western Europe).
    public static Encoding DetectEncoding(string filePath)
    {
        byte[] buffer = new byte[_encodingSampleBytes];
        int length;
        using (FileStream stream = File.OpenRead(filePath))
            length = stream.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false);

        Encoding? fromByteOrderMark = ByteOrderMarkEncoding(buffer, length);
        if (fromByteOrderMark != null)
            return fromByteOrderMark;

        try
        {
            // When the sample stops before the end of the file, a character cut in half at its end is not
            // invalid (flush: false). When it holds the whole file, an incomplete last character is (flush: true).
            bool wholeFile = length < buffer.Length;
            new UTF8Encoding(false, throwOnInvalidBytes: true).GetDecoder().GetCharCount(buffer, 0, length, flush: wholeFile);
            return new UTF8Encoding(false);
        }
        catch (DecoderFallbackException)
        {
            return Encoding.GetEncoding(1252);
        }
    }

    private static Encoding? ReadByteOrderMark(string filePath)
    {
        byte[] buffer = new byte[3];
        int length;
        using (FileStream stream = File.OpenRead(filePath))
            length = stream.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false);

        return ByteOrderMarkEncoding(buffer, length);
    }

    // The signature some programs put at the start of a file to state its encoding.
    private static Encoding? ByteOrderMarkEncoding(byte[] buffer, int length)
    {
        if (length >= 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
            return new UTF8Encoding(true);
        if (length >= 2 && buffer[0] == 0xFF && buffer[1] == 0xFE)
            return Encoding.Unicode;
        if (length >= 2 && buffer[0] == 0xFE && buffer[1] == 0xFF)
            return Encoding.BigEndianUnicode;

        return null;
    }

    private static List<string> ReadSampleLines(string filePath, Encoding encoding)
    {
        List<string> lines = [];
        using StreamReader reader = new(filePath, encoding);
        string? line;
        while (lines.Count < _separatorSampleLines && (line = reader.ReadLine()) != null)
        {
            if (!string.IsNullOrWhiteSpace(line))
                lines.Add(line);
        }
        return lines;
    }

    /// <summary>
    /// The separator that splits the sample lines most consistently: the one whose count (outside double
    /// quotes) is the same on the most lines, then the one giving more columns. Null if none appears.
    /// </summary>
    public static char? DetectSeparator(IReadOnlyList<string> sampleLines)
    {
        char? best = null;
        int bestConsistentLines = 0;
        int bestColumns = 0;

        foreach (char candidate in _candidateSeparators)
        {
            int[] counts = [.. sampleLines.Select(l => CountOutsideQuotes(l, candidate))];

            // For each separators-per-line value: how many lines have it. The most common one wins.
            var mostCommon = counts.Where(c => c > 0)
                                   .GroupBy(c => c)
                                   .Select(g => (Separators: g.Key, Lines: g.Count()))
                                   .OrderByDescending(x => x.Lines).ThenByDescending(x => x.Separators)
                                   .FirstOrDefault();
            if (mostCommon.Lines == 0)
                continue;

            if (mostCommon.Lines > bestConsistentLines || (mostCommon.Lines == bestConsistentLines && mostCommon.Separators > bestColumns))
            {
                best = candidate;
                bestConsistentLines = mostCommon.Lines;
                bestColumns = mostCommon.Separators;
            }
        }

        return best;
    }

    private static int CountOutsideQuotes(string line, char separator)
    {
        int count = 0;
        bool inQuotes = false;
        foreach (char c in line)
        {
            if (c == '"')
                inQuotes = !inQuotes;
            else if (c == separator && !inQuotes)
                count++;
        }
        return count;
    }
}
