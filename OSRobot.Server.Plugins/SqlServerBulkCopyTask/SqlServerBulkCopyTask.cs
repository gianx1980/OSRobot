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

public class SqlServerBulkCopyTask : IterationTask
{
    protected override void RunIteration(int currentIteration)
    {
        SqlServerBulkCopyTaskConfig tConfig = (SqlServerBulkCopyTaskConfig)_iterationConfig;            
        string connectionString = $"Server={tConfig.Server};Database={tConfig.Database};User ID={tConfig.Username};Password={tConfig.Password};{tConfig.ConnectionStringOptions}";

        using SqlConnection cnt = new(connectionString);
        cnt.Open();

        using SqlBulkCopy bulkCopy = new(cnt);
        bulkCopy.BulkCopyTimeout = tConfig.CommandTimeout;
        bulkCopy.DestinationTableName = tConfig.DestinationTable;
        DataTable? dtSource = (DataTable?)DynamicDataParser.GetDynamicDataObject(tConfig.SourceRecordset, _dataChain);

        if (dtSource == null)
        {
            _instanceLogger?.Error(this, "Cannot access the requested source recordset.");
            return;
        }

        _instanceLogger?.Info(this, $"About to bulk copy {dtSource.Rows.Count} rows to table {tConfig.DestinationTable}...");
        bulkCopy.WriteToServer(dtSource);
        _instanceLogger?.Info(this, "Bulk copy successfully completed");
    }
}
