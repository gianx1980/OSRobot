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

using Microsoft.Data.SqlClient;
using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using System.Data;


namespace OSRobot.Server.Plugins.SqlServerBulkCopyTask;

public class SqlServerBulkCopyTask : MultipleIterationTask
{
    protected override void RunMultipleIterationTask(int currentIteration)
    {
        SqlServerBulkCopyTaskConfig config = (SqlServerBulkCopyTaskConfig)_iterationTaskConfig;            
        string connectionString = $"Server={config.Server};Database={config.Database};User ID={config.Username};Password={config.Password};{config.ConnectionStringOptions}";

        using SqlConnection cnt = new(connectionString);
        cnt.Open();

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
        bulkCopy.WriteToServer(dtSource);
        _instanceLogger.Info(this, "Bulk copy successfully completed");
    }
}
