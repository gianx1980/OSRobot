// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using OSRobot.Server.Plugins.SqlServerFileLoaderTask;

namespace OSRobot.Tests.TestClasses;

/// <summary>The file reading and type detection of the Sql Server auto loader, without a database.</summary>
[TestClass]
public sealed class TestSqlServerFileLoaderParsing
{
    private static readonly CultureInfo Italian = CultureInfo.GetCultureInfo("it-IT");
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

    private static string TempFile(string name)
    {
        string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestSqlServerFileLoaderParsing");
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, name);
    }

    private static LoaderColumn Detect(CultureInfo culture, params object[] values)
    {
        ColumnProfile profile = new(new ValueParser(culture), detectType: true);
        foreach (object value in values)
            profile.Observe(value);
        return profile.ToColumn("Col");
    }

    // ----- Separator -------------------------------------------------------------------------------

    [TestMethod]
    [DataRow("a;b;c|1;2;3|4;5;6", ';')]
    [DataRow("a,b,c|1,2,3|4,5,6", ',')]
    [DataRow("a\tb\tc|1\t2\t3", '\t')]
    [DataRow("a|b|c", '|')]
    public void The_separator_is_detected(string lines, char expected)
    {
        // '|' separates the sample lines, except in the last row where it is the separator under test.
        string[] sample = expected == '|' ? [lines, "1|2|3"] : lines.Split('|');
        Assert.AreEqual(expected, TabularFileReader.DetectSeparator(sample));
    }

    [TestMethod]
    public void Separators_inside_double_quotes_are_ignored()
    {
        // Each line has more commas than semicolons, but all of them are inside quotes.
        string[] sample = ["\"Rossi, Mario\";\"Via Roma, 1\";10", "\"Bianchi, Anna\";\"Via Po, 2\";20"];
        Assert.AreEqual(';', TabularFileReader.DetectSeparator(sample));
    }

    [TestMethod]
    public void A_file_without_separators_has_a_single_column()
    {
        Assert.IsNull(TabularFileReader.DetectSeparator(["first", "second"]));
    }

    [TestMethod]
    public void The_most_consistent_separator_wins_over_a_more_frequent_one()
    {
        // ',' appears more often but irregularly (decimal commas); ';' splits every line into 3.
        string[] sample = ["Name;Amount;Rate", "A;1,5;2,25", "B;10;3,5", "C;7,1;1"];
        Assert.AreEqual(';', TabularFileReader.DetectSeparator(sample));
    }

    // ----- Encoding --------------------------------------------------------------------------------

    [TestMethod]
    public void The_encoding_is_detected()
    {
        string bom = TempFile("enc-bom.txt");
        File.WriteAllText(bom, "città", new UTF8Encoding(true));
        Assert.AreEqual(Encoding.UTF8.WebName, TabularFileReader.DetectEncoding(bom).WebName);

        string utf8 = TempFile("enc-utf8.txt");
        File.WriteAllText(utf8, "città", new UTF8Encoding(false));
        Assert.AreEqual(Encoding.UTF8.WebName, TabularFileReader.DetectEncoding(utf8).WebName);

        // "città" in Windows-1252: 'à' is a single byte (0xE0), invalid as UTF-8.
        string ansi = TempFile("enc-1252.txt");
        File.WriteAllBytes(ansi, [0x63, 0x69, 0x74, 0x74, 0xE0]);
        Assert.AreEqual(1252, TabularFileReader.DetectEncoding(ansi).CodePage);
    }

    // ----- Types -----------------------------------------------------------------------------------

    [TestMethod]
    public void Integers_become_int_or_bigint()
    {
        Assert.AreEqual("int", Detect(Italian, "1", "-25", "2147483647").SqlDefinition);
        Assert.AreEqual("bigint", Detect(Italian, "1", "2147483648").SqlDefinition);
    }

    [TestMethod]
    public void Decimals_follow_the_culture()
    {
        Assert.AreEqual("decimal(5, 2)", Detect(Italian, "1,5", "123,25", "-7").SqlDefinition);
        Assert.AreEqual("decimal(5, 2)", Detect(English, "1.5", "123.25", "-7").SqlDefinition);

        // In Italian "1.5" is neither a decimal (the separator is ',') nor a date: it stays text.
        Assert.AreEqual(LoaderSqlType.NVarChar, Detect(Italian, "1.5", "2.5").SqlType);
    }

    [TestMethod]
    public void Values_with_a_leading_zero_stay_text_so_codes_keep_their_zeros()
    {
        Assert.AreEqual(LoaderSqlType.NVarChar, Detect(Italian, "00123", "00456").SqlType);
        Assert.AreEqual("decimal(2, 1)", Detect(Italian, "0,5", "1,5").SqlDefinition, "A zero before the decimal separator is fine.");
    }

    [TestMethod]
    public void Dates_follow_the_culture_and_a_time_makes_them_datetime2()
    {
        Assert.AreEqual("date", Detect(Italian, "31/12/2025", "01/01/2026").SqlDefinition);
        Assert.AreEqual("date", Detect(English, "12/31/2025", "2026-01-01").SqlDefinition);
        Assert.AreEqual("datetime2", Detect(Italian, "31/12/2025 10:30", "01/01/2026").SqlDefinition);

        // 12/31/2025 is not an Italian date.
        Assert.AreEqual(LoaderSqlType.NVarChar, Detect(Italian, "12/31/2025").SqlType);
    }

    [TestMethod]
    public void True_and_false_become_bit()
    {
        Assert.AreEqual("bit", Detect(Italian, "true", "FALSE", "True").SqlDefinition);
    }

    [TestMethod]
    public void Text_columns_are_sized_from_the_longest_value()
    {
        Assert.AreEqual("nvarchar(50)", Detect(Italian, "abc").SqlDefinition);
        Assert.AreEqual("nvarchar(100)", Detect(Italian, new string('x', 51)).SqlDefinition);
        Assert.AreEqual("nvarchar(4000)", Detect(Italian, new string('x', 4000)).SqlDefinition);
        Assert.AreEqual("nvarchar(max)", Detect(Italian, new string('x', 4001)).SqlDefinition);
        Assert.AreEqual("nvarchar(50)", Detect(Italian).SqlDefinition, "A column with no values at all.");
    }

    [TestMethod]
    public void A_mix_of_types_is_text()
    {
        Assert.AreEqual(LoaderSqlType.NVarChar, Detect(Italian, "1", "abc").SqlType);
        Assert.AreEqual(LoaderSqlType.NVarChar, Detect(Italian, "1", "31/12/2025").SqlType);
    }

    [TestMethod]
    public void Without_type_detection_everything_is_text()
    {
        ColumnProfile profile = new(new ValueParser(Italian), detectType: false);
        profile.Observe("123");
        profile.Observe("31/12/2025");
        Assert.AreEqual("nvarchar(50)", profile.ToColumn("Col").SqlDefinition);
    }

    [TestMethod]
    public void Excel_values_keep_their_own_type()
    {
        Assert.AreEqual("int", Detect(Italian, 1d, 2d).SqlDefinition);
        Assert.AreEqual("decimal(3, 2)", Detect(Italian, 1.5d, 2.25d).SqlDefinition);
        Assert.AreEqual("date", Detect(Italian, new DateTime(2025, 12, 31)).SqlDefinition);
        Assert.AreEqual("bit", Detect(Italian, true, false).SqlDefinition);
    }

    [TestMethod]
    public void Converting_follows_the_detected_type()
    {
        ValueParser parser = new(Italian);
        Assert.AreEqual(1.5m, parser.Convert("1,5", new LoaderColumn("C", LoaderSqlType.Decimal, 2, 1)));
        Assert.AreEqual(new DateTime(2025, 12, 31), parser.Convert("31/12/2025", new LoaderColumn("C", LoaderSqlType.Date)));
        Assert.AreEqual(42, parser.Convert("42", new LoaderColumn("C", LoaderSqlType.Int)));
        Assert.AreEqual("00123", parser.Convert("00123", LoaderColumn.Text("C", 5)));

        ApplicationException ex = Assert.ThrowsExactly<ApplicationException>(() => parser.Convert("abc", new LoaderColumn("Amount", LoaderSqlType.Int)));
        StringAssert.Contains(ex.Message, "'Amount'");
    }

    // ----- Column names ----------------------------------------------------------------------------

    [TestMethod]
    public void Column_names_come_from_the_header_and_are_completed_and_made_unique()
    {
        List<string> names = LoaderColumnNames.Build(["Name", "", "name", "Code", null], columnCount: 6);
        CollectionAssert.AreEqual(new[] { "Name", "Column2", "name_2", "Code", "Column5", "Column6" }, names);

        CollectionAssert.AreEqual(new[] { "Column1", "Column2" }, LoaderColumnNames.Build(null, 2));
    }

    // ----- Table name ------------------------------------------------------------------------------

    [TestMethod]
    [DataRow("Customers", "[Customers]")]
    [DataRow("dbo.Customers", "[dbo].[Customers]")]
    [DataRow(" dbo . Customers ", "[dbo].[Customers]")]
    [DataRow("[My schema].[My.Table]", "[My schema].[My.Table]")]
    [DataRow("TEST.dbo.Customers", "[TEST].[dbo].[Customers]")]
    [DataRow("Odd]Name", "[Odd]]Name]")]
    [DataRow("[Odd]]Name]", "[Odd]]Name]")]
    [DataRow("[dbo] .Customers", "[dbo].[Customers]", DisplayName = "Space after a closing bracket")]
    [DataRow(" [My schema] . [My table] ", "[My schema].[My table]", DisplayName = "Spaces around bracketed names")]
    public void Table_names_are_quoted(string tableName, string expected)
    {
        Assert.AreEqual(expected, SqlTableName.Quote(tableName));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("a.b.c.d")]
    [DataRow("dbo.")]
    [DataRow("[unclosed")]
    [DataRow("[dbo]x.Customers", DisplayName = "Text after a closing bracket")]
    [DataRow("[a][b]", DisplayName = "Two bracketed names without a dot")]
    public void Invalid_table_names_are_rejected(string tableName)
    {
        Assert.ThrowsExactly<ApplicationException>(() => SqlTableName.Quote(tableName));
    }

    // ----- Files -----------------------------------------------------------------------------------

    [TestMethod]
    public void A_text_file_is_read_with_its_separator_and_quotes()
    {
        string file = TempFile("read.csv");
        File.WriteAllLines(file, ["Name;Note", "\"Rossi; Mario\";ok", "", "Bianchi;"], new UTF8Encoding(false));

        TabularFileReader reader = new(file, string.Empty);
        List<object?[]> rows = [.. reader.ReadRows()];

        Assert.IsFalse(reader.IsExcel);
        Assert.AreEqual(";", reader.Separator);
        Assert.IsTrue(reader.SeparatorDetected);
        Assert.HasCount(3, rows, "The blank line is skipped.");
        Assert.AreEqual("Rossi; Mario", rows[1][0]);
        Assert.IsNull(rows[2][1], "An empty field is null.");
    }

    [TestMethod]
    public void A_given_separator_is_used_instead_of_detecting_one()
    {
        // Detection would pick ';' (preferred on a tie); the configuration says ','.
        string file = TempFile("given.csv");
        File.WriteAllLines(file, ["a;b,c", "1;2,3"], new UTF8Encoding(false));

        TabularFileReader reader = new(file, string.Empty, separator: ",");
        List<object?[]> rows = [.. reader.ReadRows()];

        Assert.AreEqual(",", reader.Separator);
        Assert.IsFalse(reader.SeparatorDetected);
        CollectionAssert.AreEqual(new object?[] { "a;b", "c" }, rows[0]);
    }

    [TestMethod]
    [DataRow(";", ";")]
    [DataRow("||", "||", DisplayName = "More than one character")]
    [DataRow(" ", " ", DisplayName = "Space")]
    [DataRow("\\t", "\t", DisplayName = "Tab, written as \\t")]
    public void A_separator_is_read_as_written_with_a_tab_written_as_backslash_t(string written, string expected)
    {
        Assert.AreEqual(expected, TabularFileReader.ParseSeparator(written));
    }

    [TestMethod]
    public void A_text_file_needs_a_separator_when_it_is_not_detected()
    {
        string file = TempFile("nosep.csv");
        File.WriteAllLines(file, ["a;b"], new UTF8Encoding(false));

        ApplicationException ex = Assert.ThrowsExactly<ApplicationException>(() => new TabularFileReader(file, string.Empty, separator: string.Empty));
        StringAssert.Contains(ex.Message, "The separator is empty");
    }

    [TestMethod]
    public void An_excel_file_ignores_the_separator_and_encoding_settings()
    {
        string file = TempFile("nosep.xlsx");
        using (XLWorkbook workbook = new())
        {
            workbook.AddWorksheet("Data").Cell(1, 1).Value = "Name";
            workbook.SaveAs(file);
        }

        // An empty separator or encoding would be an error for a text file: here they don't matter.
        Assert.HasCount(1, new TabularFileReader(file, string.Empty, separator: string.Empty, encoding: string.Empty).ReadRows().ToList());
    }

    // ----- Configured encoding ---------------------------------------------------------------------

    private static Encoding CodePage(int codePage)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(codePage);
    }

    [TestMethod]
    public void A_configured_encoding_reads_what_detection_would_misread()
    {
        // Polish text in Windows-1250: not UTF-8, so detection falls back to Windows-1252 and garbles it.
        string file = TempFile("polish.csv");
        File.WriteAllBytes(file, CodePage(1250).GetBytes("City\r\nŁódź\r\n"));

        Assert.AreNotEqual("Łódź", new TabularFileReader(file, string.Empty).ReadRows().ElementAt(1)[0]);

        TabularFileReader reader = new(file, string.Empty, encoding: "windows-1250");
        Assert.AreEqual("Łódź", reader.ReadRows().ElementAt(1)[0]);
        Assert.IsFalse(reader.EncodingDetected);
    }

    [TestMethod]
    public void A_byte_order_mark_wins_over_the_configured_encoding()
    {
        string file = TempFile("bom-wins.csv");
        File.WriteAllText(file, "City\r\ncittà\r\n", new UTF8Encoding(true));

        TabularFileReader reader = new(file, string.Empty, encoding: "windows-1252");

        Assert.AreEqual("città", reader.ReadRows().ElementAt(1)[0]);
        Assert.AreEqual(Encoding.UTF8.WebName, reader.Encoding!.WebName);
        Assert.IsTrue(reader.EncodingDetected);
    }

    [TestMethod]
    [DataRow("", "The encoding is empty")]
    [DataRow("klingon-1", "is not a known encoding")]
    public void An_invalid_configured_encoding_is_reported(string encoding, string expectedMessage)
    {
        string file = TempFile("enc-invalid.csv");
        File.WriteAllText(file, "a;b", new UTF8Encoding(false));

        ApplicationException ex = Assert.ThrowsExactly<ApplicationException>(() => new TabularFileReader(file, string.Empty, encoding: encoding));
        StringAssert.Contains(ex.Message, expectedMessage);
    }

    [TestMethod]
    public void Every_encoding_offered_by_the_editor_is_known()
    {
        // Keep in sync with _encodingOptions in SqlServerFileLoaderTaskConfigForm.vue.
        string[] offered = ["utf-8", "windows-1252", "windows-1250", "windows-1251", "windows-1253", "iso-8859-1", "iso-8859-15", "utf-16", "utf-16BE"];

        foreach (string name in offered)
            Assert.IsNotNull(TabularFileReader.ParseEncoding(name), name);
    }

    [TestMethod]
    public void An_excel_sheet_is_read_with_typed_values()
    {
        string file = TempFile("read.xlsx");
        using (XLWorkbook workbook = new())
        {
            workbook.AddWorksheet("Other").Cell(1, 1).Value = "ignored";
            IXLWorksheet sheet = workbook.AddWorksheet("Data");
            sheet.Cell(1, 1).Value = "Name";
            sheet.Cell(1, 2).Value = "Amount";
            sheet.Cell(1, 3).Value = "Date";
            sheet.Cell(2, 1).Value = "Rossi";
            sheet.Cell(2, 2).Value = 1.5;
            sheet.Cell(2, 3).Value = new DateTime(2025, 12, 31);
            workbook.SaveAs(file);
        }

        List<object?[]> rows = [.. new TabularFileReader(file, "Data").ReadRows()];

        Assert.HasCount(2, rows);
        Assert.AreEqual("Rossi", rows[1][0]);
        Assert.AreEqual(1.5d, rows[1][1]);
        Assert.AreEqual(new DateTime(2025, 12, 31), rows[1][2]);

        ApplicationException ex = Assert.ThrowsExactly<ApplicationException>(() => new TabularFileReader(file, "Missing").ReadRows().ToList());
        StringAssert.Contains(ex.Message, "sheet 'Missing' does not exist");
    }

    [TestMethod]
    public void The_old_excel_format_is_rejected_with_a_clear_message()
    {
        string file = TempFile("old.xls");
        File.WriteAllBytes(file, [0]);

        ApplicationException ex = Assert.ThrowsExactly<ApplicationException>(() => new TabularFileReader(file, string.Empty));
        StringAssert.Contains(ex.Message, ".xlsx");
    }
}
