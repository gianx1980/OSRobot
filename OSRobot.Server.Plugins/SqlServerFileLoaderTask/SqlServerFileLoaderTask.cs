// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.Data.SqlClient;
using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using System.Data;
using System.Globalization;

namespace OSRobot.Server.Plugins.SqlServerFileLoaderTask;

/// <summary>
/// Loads a delimited text file or an Excel sheet into a SQL Server table, creating the table from the file:
/// - a first pass over the file finds the columns and, optionally, their types;
/// - the table is created (or appended to, or recreated) and the rows are bulk copied in batches,
///   so a large text file is never held in memory;
/// - everything happens in one transaction: if anything fails, the database is left as it was.
/// </summary>
public class SqlServerFileLoaderTask : MultipleIterationTask
{
    private const int _batchSize = 10000;

    private long _rowsLoaded;
    private bool _tableCreated;
    private string _tableName = string.Empty;

    private void LogInfo(string message)
    {
        if (Config.Log)
            _instanceLogger.Info(this, message);
    }

    protected override async Task RunMultipleIterationTaskAsync(int currentIteration)
    {
        SqlServerFileLoaderTaskConfig config = (SqlServerFileLoaderTaskConfig)_iterationTaskConfig;
        _rowsLoaded = 0;
        _tableCreated = false;
        _tableName = config.TableName;

        if (string.IsNullOrWhiteSpace(config.TableName))
            throw new ApplicationException("The table name is empty.");

        string quotedTableName = SqlTableName.Quote(config.TableName);
        ValueParser parser = new(GetCulture(config.Culture));

        TabularFileReader reader = new(config.FilePath, config.SheetName,
                                       separator: config.AutodetectSeparator ? null : config.Separator,
                                       encoding: config.AutodetectEncoding ? null : config.Encoding);
        LogInfo(reader.IsExcel
            ? $"Reading Excel file '{config.FilePath}'{(string.IsNullOrEmpty(config.SheetName) ? string.Empty : $", sheet '{config.SheetName}'")}..."
            : $"Reading text file '{config.FilePath}' (encoding: {reader.Encoding!.WebName}{(reader.EncodingDetected ? ", detected" : string.Empty)}, separator: {DescribeSeparator(reader.Separator)}{(reader.SeparatorDetected ? ", detected" : string.Empty)})...");

        List<LoaderColumn> columns = DetectColumns(reader, parser, config);
        LogInfo($"Columns: {string.Join(", ", columns)}");

        string connectionString = $"Server={config.Server};Database={config.Database};User ID={config.Username};Password={config.Password};{config.ConnectionStringOptions}";
        using SqlConnection cnt = new(connectionString);
        await cnt.OpenAsync(_cancellationToken);

        // Disposed without a commit (an exception, a cancellation): everything below is rolled back.
        using SqlTransaction transaction = (SqlTransaction)await cnt.BeginTransactionAsync(_cancellationToken);

        List<string> destinationColumns = await PrepareTableAsync(cnt, transaction, quotedTableName, columns, config);
        await BulkCopyAsync(cnt, transaction, quotedTableName, reader, parser, columns, destinationColumns, config);

        await transaction.CommitAsync(_cancellationToken);
        LogInfo($"{_rowsLoaded} row(s) loaded into {quotedTableName}.");
    }

    private List<LoaderColumn> DetectColumns(TabularFileReader reader, ValueParser parser, SqlServerFileLoaderTaskConfig config)
    {
        object?[]? headerRow = null;
        List<ColumnProfile> profiles = [];
        bool firstRow = true;

        foreach (object?[] row in reader.ReadRows())
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (firstRow && config.FirstRowContainsColumnNames)
            {
                headerRow = row;
                firstRow = false;
                continue;
            }
            firstRow = false;

            while (profiles.Count < row.Length)
                profiles.Add(new ColumnProfile(parser, config.AutodetectColumnTypes));

            for (int i = 0; i < row.Length; i++)
                profiles[i].Observe(row[i]);
        }

        int columnCount = Math.Max(headerRow?.Length ?? 0, profiles.Count);
        if (columnCount == 0)
            throw new ApplicationException($"File '{config.FilePath}' is empty.");

        while (profiles.Count < columnCount)
            profiles.Add(new ColumnProfile(parser, config.AutodetectColumnTypes));

        List<string> names = LoaderColumnNames.Build(headerRow, columnCount);
        return [.. profiles.Select((profile, i) => profile.ToColumn(names[i]))];
    }

    /// <summary>Creates, recreates or checks the destination table. Returns the destination column of each file column.</summary>
    private async Task<List<string>> PrepareTableAsync(SqlConnection cnt, SqlTransaction transaction, string quotedTableName,
                                                       List<LoaderColumn> columns, SqlServerFileLoaderTaskConfig config)
    {
        bool exists;
        using (SqlCommand cmdExists = new("SELECT OBJECT_ID(@tableName, N'U')", cnt, transaction) { CommandTimeout = config.CommandTimeout })
        {
            cmdExists.Parameters.AddWithValue("@tableName", quotedTableName);
            exists = await cmdExists.ExecuteScalarAsync(_cancellationToken) is not (null or DBNull);
        }

        if (exists)
        {
            switch (config.IfTableExists)
            {
                case IfTableExistsAction.Append:
                    LogInfo($"Table {quotedTableName} exists: appending the rows.");
                    return await MapToExistingColumnsAsync(cnt, transaction, quotedTableName, columns, config);

                case IfTableExistsAction.DropAndRecreate:
                    LogInfo($"Table {quotedTableName} exists: dropping it.");
                    await ExecuteAsync(cnt, transaction, $"DROP TABLE {quotedTableName}", config);
                    break;

                default:
                    throw new ApplicationException($"Table {quotedTableName} already exists. To load into it anyway, set 'If the table exists' to Append or Drop and recreate.");
            }
        }

        string columnDefinitions = string.Join(", ", columns.Select(c => $"{SqlTableName.QuoteIdentifier(c.Name)} {c.SqlDefinition} NULL"));
        await ExecuteAsync(cnt, transaction, $"CREATE TABLE {quotedTableName} ({columnDefinitions})", config);
        _tableCreated = true;
        LogInfo($"Table {quotedTableName} created.");

        return [.. columns.Select(c => c.Name)];
    }

    // Matched by name, ignoring case like SQL Server does: the bulk copy column mappings themselves are case-sensitive.
    private async Task<List<string>> MapToExistingColumnsAsync(SqlConnection cnt, SqlTransaction transaction, string quotedTableName,
                                                                List<LoaderColumn> columns, SqlServerFileLoaderTaskConfig config)
    {
        List<string> existing = [];
        using (SqlCommand cmdColumns = new("SELECT name FROM sys.columns WHERE object_id = OBJECT_ID(@tableName) ORDER BY column_id", cnt, transaction)
               { CommandTimeout = config.CommandTimeout })
        {
            cmdColumns.Parameters.AddWithValue("@tableName", quotedTableName);
            using SqlDataReader dataReader = await cmdColumns.ExecuteReaderAsync(_cancellationToken);
            while (await dataReader.ReadAsync(_cancellationToken))
                existing.Add(dataReader.GetString(0));
        }

        List<string> mapped = [];
        List<string> missing = [];
        foreach (LoaderColumn column in columns)
        {
            string? destination = existing.FirstOrDefault(e => string.Equals(e, column.Name, StringComparison.OrdinalIgnoreCase));
            if (destination == null)
                missing.Add(column.Name);
            else
                mapped.Add(destination);
        }

        if (missing.Count > 0)
            throw new ApplicationException($"Table {quotedTableName} has no column {string.Join(", ", missing.Select(m => $"'{m}'"))}. " +
                                           $"Its columns are: {string.Join(", ", existing)}.");

        return mapped;
    }

    private async Task BulkCopyAsync(SqlConnection cnt, SqlTransaction transaction, string quotedTableName, TabularFileReader reader,
                                     ValueParser parser, List<LoaderColumn> columns, List<string> destinationColumns,
                                     SqlServerFileLoaderTaskConfig config)
    {
        using SqlBulkCopy bulkCopy = new(cnt, SqlBulkCopyOptions.Default, transaction)
        {
            DestinationTableName = quotedTableName,
            BulkCopyTimeout = config.CommandTimeout
        };

        using DataTable batch = new();
        for (int i = 0; i < columns.Count; i++)
        {
            batch.Columns.Add(columns[i].Name, columns[i].ClrType);
            bulkCopy.ColumnMappings.Add(columns[i].Name, destinationColumns[i]);
        }

        long rowNumber = 0;
        foreach (object?[] row in reader.ReadRows())
        {
            _cancellationToken.ThrowIfCancellationRequested();
            rowNumber++;

            if (rowNumber == 1 && config.FirstRowContainsColumnNames)
                continue;

            object[] values = new object[columns.Count];
            for (int i = 0; i < columns.Count; i++)
            {
                object? value = i < row.Length ? row[i] : null;
                try
                {
                    values[i] = value == null ? DBNull.Value : parser.Convert(value, columns[i]);
                }
                catch (ApplicationException ex)
                {
                    throw new ApplicationException($"Row {rowNumber}: {ex.Message}", ex);
                }
            }

            batch.Rows.Add(values);
            _rowsLoaded++;

            if (batch.Rows.Count == _batchSize)
            {
                await bulkCopy.WriteToServerAsync(batch, _cancellationToken);
                batch.Clear();
            }
        }

        if (batch.Rows.Count > 0)
            await bulkCopy.WriteToServerAsync(batch, _cancellationToken);
    }

    private async Task ExecuteAsync(SqlConnection cnt, SqlTransaction transaction, string sql, SqlServerFileLoaderTaskConfig config)
    {
        using SqlCommand cmd = new(sql, cnt, transaction) { CommandTimeout = config.CommandTimeout };
        await cmd.ExecuteNonQueryAsync(_cancellationToken);
    }

    private static CultureInfo GetCulture(string culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
            return CultureInfo.CurrentCulture;

        try
        {
            return CultureInfo.GetCultureInfo(culture.Trim());
        }
        catch (CultureNotFoundException ex)
        {
            throw new ApplicationException($"'{culture}' is not a valid culture: use a name such as it-IT or en-US, or leave it empty for the server's settings.", ex);
        }
    }

    private static string DescribeSeparator(string? separator) => separator switch
    {
        null => "none, single column",
        "\t" => "tab",
        _ => $"'{separator}'"
    };

    private void PostIteration(DynamicDataSet dDataSet)
    {
        dDataSet.TryAdd(SqlServerFileLoaderTaskCommon.DynDataKeyRowsLoaded, _rowsLoaded);
        dDataSet.TryAdd(SqlServerFileLoaderTaskCommon.DynDataKeyTableName, _tableName);
        dDataSet.TryAdd(SqlServerFileLoaderTaskCommon.DynDataKeyTableCreated, _tableCreated);
    }

    protected override void PostTaskSucceded(int currentIteration, ExecResult result, DynamicDataSet dDataSet) => PostIteration(dDataSet);

    // On failure the transaction was rolled back: nothing was loaded or created, whatever the counters said.
    protected override void PostTaskFailed(int currentIteration, ExecResult result, DynamicDataSet dDataSet)
    {
        _rowsLoaded = 0;
        _tableCreated = false;
        PostIteration(dDataSet);
    }
}
