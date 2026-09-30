// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.Data.SqlClient;
using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using System.Data;


namespace OSRobot.Server.Plugins.SqlServerBulkCopyTask;

public class SqlServerBulkCopyTask : MultipleIterationTask
{
    protected override async Task RunMultipleIterationTaskAsync(int currentIteration)
    {
        SqlServerBulkCopyTaskConfig config = (SqlServerBulkCopyTaskConfig)_iterationTaskConfig;
        string connectionString = $"Server={config.Server};Database={config.Database};User ID={config.Username};Password={config.Password};{config.ConnectionStringOptions}";

        using SqlConnection cnt = new(connectionString);
        await cnt.OpenAsync(_cancellationToken);

        using SqlBulkCopy bulkCopy = new(cnt);
        bulkCopy.BulkCopyTimeout = config.CommandTimeout;
        bulkCopy.DestinationTableName = config.DestinationTable;
        DataTable? dtSource = (DataTable?)DynamicDataParser.GetDynamicDataObject(config.SourceRecordset, _dataChain);

        if (dtSource == null)
        {
            _instanceLogger.Error(this, $"Cannot access the requested source recordset {config.SourceRecordset}.");
            throw new ApplicationException("Cannot access the requested source recordset.");
        }

        _instanceLogger.Info(this, $"About to bulk copy {dtSource.Rows.Count} rows to table {config.DestinationTable}...");
        await bulkCopy.WriteToServerAsync(dtSource, _cancellationToken);
        _instanceLogger.Info(this, "Bulk copy successfully completed");
    }
}
