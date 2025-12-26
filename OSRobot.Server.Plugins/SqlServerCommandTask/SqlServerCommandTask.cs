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

using System.Data;
using Microsoft.Data.SqlClient;
using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;

namespace OSRobot.Server.Plugins.SqlServerCommandTask;

public class SqlServerCommandTask : MultipleIterationTask
{
    private int _executionReturnValue;

    private SqlParameter CreateParameter(SqlServerParamDefinition paramDef, DynamicDataChain dataChain, int iterationNumber)
    {
        SqlParameter sqlParam = new()
        {
            ParameterName = paramDef.Name
        };

        switch (paramDef.Type)
        {
            case SqlParamType.Varchar:
                sqlParam.SqlDbType = SqlDbType.VarChar;
                if (!string.IsNullOrEmpty(paramDef.Length))
                {
                    if (!paramDef.Length.Equals("MAX", StringComparison.CurrentCultureIgnoreCase))
                        sqlParam.Size = int.Parse(paramDef.Length);
                    else
                        sqlParam.Size = -1;
                }
                    
                break;

            case SqlParamType.NVarchar:
                sqlParam.SqlDbType = SqlDbType.NVarChar;
                if (!string.IsNullOrEmpty(paramDef.Length))
                {
                    if (!paramDef.Length.Equals("MAX", StringComparison.CurrentCultureIgnoreCase))
                        sqlParam.Size = int.Parse(paramDef.Length);
                    else
                        sqlParam.Size = -1;
                }
                break;

            case SqlParamType.Xml:
                sqlParam.SqlDbType = SqlDbType.Xml;
                sqlParam.Size = -1;
                break;

            case SqlParamType.Numeric:
                sqlParam.SqlDbType = SqlDbType.Decimal;
                if (!string.IsNullOrEmpty(paramDef.Length))
                    sqlParam.Size = int.Parse(paramDef.Length);
                if (!string.IsNullOrEmpty(paramDef.Precision))
                    sqlParam.Precision = byte.Parse(paramDef.Precision);
                break;

            case SqlParamType.Int:
                sqlParam.SqlDbType = SqlDbType.Int;
                break;

            case SqlParamType.Long:
                sqlParam.SqlDbType = SqlDbType.BigInt;
                break;

            case SqlParamType.Bit:
                sqlParam.SqlDbType = SqlDbType.Bit;
                break;

            case SqlParamType.Date:
                sqlParam.SqlDbType = SqlDbType.Date;
                break;

            case SqlParamType.Datetime:
                sqlParam.SqlDbType = SqlDbType.DateTime;
                break;

            case SqlParamType.VarBinary:
                sqlParam.SqlDbType = SqlDbType.VarBinary;
                if (!string.IsNullOrEmpty(paramDef.Length))
                {
                    if (!paramDef.Length.Equals("MAX", StringComparison.CurrentCultureIgnoreCase))
                        sqlParam.Size = int.Parse(paramDef.Length);
                    else
                        sqlParam.Size = -1;
                }

                // The varbinary type is handled differently from the others and therefore requires separate handling.
                List<DynamicDataInfo> dynDataInfoList = DynamicDataParser.GetDynamicDataInfo(paramDef.Value);
                if (dynDataInfoList.Count > 1)
                    throw new ApplicationException("Multiple dynamic data are note allowed for Varbinary parameters.");

                DynamicDataInfo dynDataInfo = dynDataInfoList[0];
                object? paramValue = DynamicDataParser.GetDynamicDataValue(dynDataInfo, dataChain, iterationNumber, _subInstanceIndex);
                sqlParam.Value = paramValue ?? DBNull.Value;
                return sqlParam;
        }

        sqlParam.Value = DynamicDataParser.ReplaceDynamicData(paramDef.Value, dataChain, iterationNumber, _subInstanceIndex);

        return sqlParam;
    }

    protected override void RunMultipleIterationTask(int currentIteration)
    {
        SqlServerCommandTaskConfig config = (SqlServerCommandTaskConfig)_iterationTaskConfig;
        _defaultRecordset = new DataTable();

        string ConnectionString = $"Server={config.Server};Database={config.Database};User Id={config.Username};Password={config.Password};{config.ConnectionStringOptions}";

        using SqlConnection cnt = new(ConnectionString);
        using SqlCommand cmd = new(string.Empty, cnt);
        cnt.Open();

        if (config.Type == QueryTaskType.Text)
            cmd.CommandType = CommandType.Text;
        else
            cmd.CommandType = CommandType.StoredProcedure;

        if (cmd.CommandType == CommandType.StoredProcedure)
        {
            cmd.Parameters.Add("@RETVALUE", SqlDbType.Int);
            cmd.Parameters["@RETVALUE"].Direction = ParameterDirection.ReturnValue;
        }

        foreach (SqlServerParamDefinition ParamDef in config.ParamsDefinition)
        {
            cmd.Parameters.Add(CreateParameter(ParamDef, _dataChain, currentIteration));
        }

        cmd.CommandText = config.Query;
        cmd.CommandTimeout = config.CommandTimeout;

        if (config.ReturnsRecordset)
        {
            SqlDataAdapter da = new(cmd);
            da.Fill((DataTable)_defaultRecordset);
        }
        else
        {
            _executionReturnValue = cmd.ExecuteNonQuery();
        }

        if (cmd.CommandType == CommandType.StoredProcedure)
        {
            _executionReturnValue = (int)cmd.Parameters["@RETVALUE"].Value;
        }
    }

    private void PostIteration(int currentIteration, ExecResult result, DynamicDataSet dDataSet)
    {
        SqlServerCommandTaskConfig config = (SqlServerCommandTaskConfig)_iterationTaskConfig;

        if (config.Log)
        {
            _instanceLogger.Info(this, $"Number of queries/commands executed: {currentIteration + 1}");
            _instanceLogger.TaskCompleted(this);
        }

        if (config.ReturnsRecordset)
            dDataSet.TryAdd(CommonDynamicData.DefaultRecordsetName, _defaultRecordset);

        dDataSet[CommonDynamicData.ExecutionReturnValue] = _executionReturnValue;
    }

    protected override void PostTaskSucceded(int currentIteration, ExecResult result, DynamicDataSet dDataSet)
    {
        PostIteration(currentIteration, result, dDataSet);
    }

    protected override void PostTaskFailed(int currentIteration, ExecResult result, DynamicDataSet dDataSet)
    {
        PostIteration(currentIteration, result, dDataSet);
    }
}
