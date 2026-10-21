// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text;
using ClosedXML.Excel;
using Microsoft.Data.SqlClient;
using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging.Abstract;
using OSRobot.Server.Plugins.SqlServerFileLoaderTask;

namespace OSRobot.Tests.TestPlugins;

/// <summary>The Sql Server auto loader against the local test database, like the other Sql Server task tests.</summary>
[TestClass]
public sealed class TestSqlServerFileLoaderTask
{
    private const string ConnectionString = "Server=(local);Database=TEST;User ID=Test;Password=12345;Encrypt=no";

    private string _table = string.Empty;
    private string _folder = string.Empty;

    [TestInitialize]
    public void CreateNames()
    {
        string id = Guid.NewGuid().ToString("N")[..12];
        _table = $"TEST_FILELOADER_{id}";
        _folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestSqlServerFileLoaderTask", id);
        Directory.CreateDirectory(_folder);
    }

    [TestCleanup]
    public void DropTable() => Execute($"DROP TABLE IF EXISTS [{_table}]");

    private string WriteFile(string name, params string[] lines)
    {
        string path = Path.Combine(_folder, name);
        File.WriteAllLines(path, lines, new UTF8Encoding(false));
        return path;
    }

    private SqlServerFileLoaderTaskConfig Config(string filePath, IfTableExistsAction ifTableExists = IfTableExistsAction.Fail,
                                                 bool autodetect = true, string culture = "it-IT") => new()
    {
        Id = 1,
        Name = "File loader",
        Server = "(local)",
        Database = "TEST",
        Username = "Test",
        Password = "12345",
        ConnectionStringOptions = "Encrypt=no",
        FilePath = filePath,
        TableName = $"dbo.{_table}",
        AutodetectColumnTypes = autodetect,
        Culture = culture,
        IfTableExists = ifTableExists,
        PluginIterationMode = IterationMode.IterateExactNumber,
        IterationsCount = 1
    };

    private static async Task<ExecResult> RunAsync(SqlServerFileLoaderTaskConfig config)
    {
        SqlServerFileLoaderTask task = new() { Config = config, ParentFolder = Common.CreateRootFolder() };

        Common.ConfigureLogPath();
        (DynamicDataChain dynDataChain, DynamicDataSet dynDataSet, IPluginInstanceLogger logger) = Common.GetTaskDefaultParameters(task);

        task.Init();
        try
        {
            return (await task.RunAsync(dynDataChain, dynDataSet, 0, logger, CancellationToken.None)).ExecResults.Single();
        }
        finally
        {
            task.Destroy();
        }
    }

    private static void Execute(string sql)
    {
        using SqlConnection cnt = new(ConnectionString);
        cnt.Open();
        using SqlCommand cmd = new(sql, cnt);
        cmd.ExecuteNonQuery();
    }

    private static object? Scalar(string sql)
    {
        using SqlConnection cnt = new(ConnectionString);
        cnt.Open();
        using SqlCommand cmd = new(sql, cnt);
        return cmd.ExecuteScalar();
    }

    private int RowCount() => (int)Scalar($"SELECT COUNT(*) FROM [{_table}]")!;

    /// <summary>The table's columns as "Name type", e.g. "Amount decimal(3, 1)".</summary>
    private List<string> ColumnDefinitions()
    {
        const string sql = """
            SELECT c.COLUMN_NAME, c.DATA_TYPE, c.CHARACTER_MAXIMUM_LENGTH, c.NUMERIC_PRECISION, c.NUMERIC_SCALE
            FROM INFORMATION_SCHEMA.COLUMNS c
            WHERE c.TABLE_SCHEMA = 'dbo' AND c.TABLE_NAME = @table
            ORDER BY c.ORDINAL_POSITION
            """;
        using SqlConnection cnt = new(ConnectionString);
        cnt.Open();
        using SqlCommand cmd = new(sql, cnt);
        cmd.Parameters.AddWithValue("@table", _table);
        using SqlDataReader reader = cmd.ExecuteReader();

        List<string> columns = [];
        while (reader.Read())
        {
            string type = reader.GetString(1);
            if (type == "nvarchar")
                type += reader.GetInt32(2) == -1 ? "(max)" : $"({reader.GetInt32(2)})";
            else if (type == "decimal")
                type += $"({reader.GetByte(3)}, {reader.GetInt32(4)})";
            columns.Add($"{reader.GetString(0)} {type}");
        }
        return columns;
    }

    private string ItalianFile(string name = "customers.csv") => WriteFile(name,
        "Code;Name;Amount;Date;Active",
        "00123;Rossi;1,5;31/12/2025;true",
        "00456;Bianchi;10;01/01/2026;false");

    [TestMethod]
    public async Task A_semicolon_file_with_italian_numbers_and_dates_is_loaded_into_a_new_table()
    {
        ExecResult result = await RunAsync(Config(ItalianFile()));

        Assert.IsTrue(result.Result, "The load failed.");
        Assert.AreEqual(2L, result.Data[SqlServerFileLoaderTaskCommon.DynDataKeyRowsLoaded]);
        Assert.IsTrue((bool)result.Data[SqlServerFileLoaderTaskCommon.DynDataKeyTableCreated]);

        CollectionAssert.AreEqual(new[] { "Code nvarchar(50)", "Name nvarchar(50)", "Amount decimal(3, 1)", "Date date", "Active bit" },
                                  ColumnDefinitions());

        Assert.AreEqual(11.5m, Scalar($"SELECT SUM(Amount) FROM [{_table}]"));
        Assert.AreEqual("00123", Scalar($"SELECT Code FROM [{_table}] WHERE Name = 'Rossi'"), "Codes keep their leading zeros.");
        Assert.AreEqual(new DateTime(2025, 12, 31), Scalar($"SELECT [Date] FROM [{_table}] WHERE Name = 'Rossi'"));
        Assert.AreEqual(1, Scalar($"SELECT COUNT(*) FROM [{_table}] WHERE Active = 1"));
    }

    [TestMethod]
    public async Task An_excel_file_is_loaded_with_the_cell_types()
    {
        string file = Path.Combine(_folder, "orders.xlsx");
        using (XLWorkbook workbook = new())
        {
            IXLWorksheet sheet = workbook.AddWorksheet("Orders");
            sheet.Cell(1, 1).Value = "Order";
            sheet.Cell(1, 2).Value = "Total";
            sheet.Cell(1, 3).Value = "Shipped";
            for (int i = 1; i <= 3; i++)
            {
                sheet.Cell(i + 1, 1).Value = i;
                sheet.Cell(i + 1, 2).Value = i * 10.25;
                sheet.Cell(i + 1, 3).Value = new DateTime(2025, 1, i);
            }
            workbook.SaveAs(file);
        }

        ExecResult result = await RunAsync(Config(file));

        Assert.IsTrue(result.Result, "The load failed.");
        CollectionAssert.AreEqual(new[] { "Order int", "Total decimal(4, 2)", "Shipped date" }, ColumnDefinitions());
        Assert.AreEqual(61.5m, Scalar($"SELECT SUM(Total) FROM [{_table}]"));
    }

    [TestMethod]
    public async Task Without_type_detection_every_column_is_text()
    {
        ExecResult result = await RunAsync(Config(ItalianFile(), autodetect: false));

        Assert.IsTrue(result.Result, "The load failed.");
        Assert.IsTrue(ColumnDefinitions().All(c => c.EndsWith("nvarchar(50)")), string.Join(", ", ColumnDefinitions()));
        Assert.AreEqual("1,5", Scalar($"SELECT Amount FROM [{_table}] WHERE Name = 'Rossi'"), "Values are loaded as written in the file.");
    }

    [TestMethod]
    public async Task An_existing_table_makes_the_task_fail_by_default_and_is_left_untouched()
    {
        string file = ItalianFile();
        Assert.IsTrue((await RunAsync(Config(file))).Result);

        ExecResult second = await RunAsync(Config(file));

        Assert.IsFalse(second.Result, "Loading into an existing table must fail with IfTableExists = Fail.");
        Assert.AreEqual(0L, second.Data[SqlServerFileLoaderTaskCommon.DynDataKeyRowsLoaded]);
        Assert.AreEqual(2, RowCount());
    }

    [TestMethod]
    public async Task Append_adds_the_rows_to_the_existing_table_matching_columns_by_name()
    {
        Assert.IsTrue((await RunAsync(Config(ItalianFile()))).Result);

        // Different column order and case: matched by name, ignoring case.
        string more = WriteFile("more.csv", "active;AMOUNT;Name;Code;Date", "true;2,5;Verdi;00789;15/06/2026");
        ExecResult second = await RunAsync(Config(more, IfTableExistsAction.Append));

        Assert.IsTrue(second.Result, "The append failed.");
        Assert.IsFalse((bool)second.Data[SqlServerFileLoaderTaskCommon.DynDataKeyTableCreated]);
        Assert.AreEqual(3, RowCount());
        Assert.AreEqual(14m, Scalar($"SELECT SUM(Amount) FROM [{_table}]"));
    }

    [TestMethod]
    public async Task Append_fails_with_a_clear_error_when_a_column_is_missing_from_the_table()
    {
        Assert.IsTrue((await RunAsync(Config(ItalianFile()))).Result);

        string other = WriteFile("other.csv", "Code;Unknown", "00999;x");
        Assert.IsFalse((await RunAsync(Config(other, IfTableExistsAction.Append))).Result);
        Assert.AreEqual(2, RowCount());
    }

    [TestMethod]
    public async Task A_failed_append_is_rolled_back_entirely()
    {
        Assert.IsTrue((await RunAsync(Config(ItalianFile()))).Result);

        // The first row is fine, the second can't go into the decimal Amount column: nothing must be kept.
        string bad = WriteFile("bad.csv", "Code;Name;Amount;Date;Active", "00111;Neri;3;01/02/2026;true", "00222;Gialli;abc;01/02/2026;true");
        Assert.IsFalse((await RunAsync(Config(bad, IfTableExistsAction.Append))).Result);

        Assert.AreEqual(2, RowCount(), "The rows of a failed load must not be kept.");
    }

    [TestMethod]
    public async Task Drop_and_recreate_replaces_the_table_with_the_new_file()
    {
        Assert.IsTrue((await RunAsync(Config(ItalianFile()))).Result);

        string other = WriteFile("other.csv", "Id;Description", "1;first", "2;second", "3;third");
        ExecResult second = await RunAsync(Config(other, IfTableExistsAction.DropAndRecreate));

        Assert.IsTrue(second.Result, "The reload failed.");
        Assert.IsTrue((bool)second.Data[SqlServerFileLoaderTaskCommon.DynDataKeyTableCreated]);
        CollectionAssert.AreEqual(new[] { "Id int", "Description nvarchar(50)" }, ColumnDefinitions());
        Assert.AreEqual(3, RowCount());
    }

    [TestMethod]
    public async Task A_given_separator_is_used_to_split_the_columns()
    {
        // Values contain ';' and ',': only the configured '|' separates columns.
        string file = WriteFile("pipe.txt", "Name|Note", "Rossi|a;b,c", "Bianchi|d");
        SqlServerFileLoaderTaskConfig config = Config(file);
        config.AutodetectSeparator = false;
        config.Separator = "|";

        Assert.IsTrue((await RunAsync(config)).Result, "The load failed.");
        CollectionAssert.AreEqual(new[] { "Name nvarchar(50)", "Note nvarchar(50)" }, ColumnDefinitions());
        Assert.AreEqual("a;b,c", Scalar($"SELECT Note FROM [{_table}] WHERE Name = 'Rossi'"));
    }

    [TestMethod]
    public async Task Without_autodetection_an_empty_separator_fails_the_task_and_creates_nothing()
    {
        SqlServerFileLoaderTaskConfig config = Config(ItalianFile());
        config.AutodetectSeparator = false;
        config.Separator = string.Empty;

        Assert.IsFalse((await RunAsync(config)).Result);
        Assert.AreEqual(DBNull.Value, Scalar($"SELECT OBJECT_ID('dbo.{_table}', 'U')"), "No table must be created.");
    }

    [TestMethod]
    public async Task A_configured_encoding_loads_the_text_correctly()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        string file = Path.Combine(_folder, "polish.csv");
        File.WriteAllBytes(file, Encoding.GetEncoding(1250).GetBytes("Id;City\r\n1;Łódź\r\n2;Kraków\r\n"));

        SqlServerFileLoaderTaskConfig config = Config(file);
        config.AutodetectEncoding = false;
        config.Encoding = "windows-1250";

        Assert.IsTrue((await RunAsync(config)).Result, "The load failed.");
        Assert.AreEqual("Łódź", Scalar($"SELECT City FROM [{_table}] WHERE Id = 1"));
        Assert.AreEqual("Kraków", Scalar($"SELECT City FROM [{_table}] WHERE Id = 2"));
    }

    [TestMethod]
    public async Task More_rows_than_a_batch_are_all_loaded()
    {
        string[] lines = ["Id;Value", .. Enumerable.Range(1, 25_000).Select(i => $"{i};v{i}")];
        ExecResult result = await RunAsync(Config(WriteFile("big.csv", lines)));

        Assert.IsTrue(result.Result, "The load failed.");
        Assert.AreEqual(25_000, RowCount());
        Assert.AreEqual(25_000L * 25_001 / 2, Convert.ToInt64(Scalar($"SELECT SUM(CAST(Id AS bigint)) FROM [{_table}]")));
    }
}
