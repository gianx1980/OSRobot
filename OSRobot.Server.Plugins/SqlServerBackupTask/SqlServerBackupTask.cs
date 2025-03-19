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
using OSRobot.Server.Core.Logging.Abstract;
using OSRobot.Server.Plugins.Infrastructure.Utilities;
using System.Data;

namespace OSRobot.Server.Plugins.SqlServerBackupTask;

public class SqlServerBackupTask : IterationTask
{
    private const int _backupSuccess = -1;
    private const int _backupSuccessChecksumError = -2;
    private const int _backupError = -3;

    private int _successfulBackupsNumber = 0;
    private int _failedBackupsNumber = 0;

    private int BackupDatabase(string connectionString, string databaseName, string destination, string fileName, bool overwriteIfExists,
                                    bool checksum, bool continueOnError, string mediaName, UseCompressionEnum compression,
                                    IPluginInstanceLogger? logger)
    {
        int Result = _backupError;

        try
        {
            string FullPathDestination;
            if (string.IsNullOrEmpty(fileName.Trim()))
                FullPathDestination = Path.Combine(destination, databaseName + ".bak");
            else
                FullPathDestination = Path.Combine(destination, fileName);

            string SqlCommandBackup = @"
                DECLARE @DBNAME      NVARCHAR(1000) = @P_DBNAME
                DECLARE @PATH        NVARCHAR(1000) = @P_PATH
                DECLARE @MEDIANAME   NVARCHAR(1000) = @P_MEDIANAME

                BACKUP DATABASE @DBNAME TO  DISK = @PATH WITH NAME = @MEDIANAME
                ";

            if (overwriteIfExists)
                SqlCommandBackup += ", INIT";

            if (checksum)
                SqlCommandBackup += ", CHECKSUM";

            if (continueOnError)
                SqlCommandBackup += ", CONTINUE_AFTER_ERROR";

            if (compression == UseCompressionEnum.CompressBackup)
                SqlCommandBackup += ", COMPRESSION";
            else if (compression == UseCompressionEnum.DoNotCompressBackup)
                SqlCommandBackup += ", NO_COMPRESSION";

            using SqlConnection Cnt = new(connectionString);
            ManualResetEvent WaitInfoMessage = new(false);
            Cnt.InfoMessage += (sender, e) =>
            {
                if (e.Message.Contains("BACKUP WITH CONTINUE_AFTER_ERROR successfully"))
                    Result = _backupSuccessChecksumError;
                else if (e.Message.Contains("BACKUP DATABASE successfully"))
                    Result = _backupSuccess;

                WaitInfoMessage.Set();
            };

            Cnt.Open();

            using SqlCommand Cmd = new(SqlCommandBackup, Cnt);
            Cmd.Parameters.Add("@P_DBNAME", SqlDbType.NVarChar).Value = databaseName;
            Cmd.Parameters.Add("@P_PATH", SqlDbType.NVarChar).Value = FullPathDestination;
            Cmd.Parameters.Add("@P_MEDIANAME", SqlDbType.NVarChar).Value = mediaName;
            Cmd.ExecuteNonQuery();
            WaitInfoMessage.WaitOne();
        }
        catch (Exception ex)
        {
            Result = _backupError;
            logger?.Error(this, $"Backup of database {databaseName} failed", ex);
        }

        return Result;
    }

    private int BackupTransactionLog(string connectionString, string databaseName, string destination, string fileName, bool overwriteIfExists,
                                        bool checksum, bool continueOnError, string mediaName, UseCompressionEnum compression,
                                        IPluginInstanceLogger? logger)
    {
        int result = _backupError;

        try
        {
            string fullPathDestination;
            if (string.IsNullOrEmpty(fileName.Trim()))
                fullPathDestination = Path.Combine(destination, databaseName + ".trn");
            else
                fullPathDestination = Path.Combine(destination, fileName);

            string sqlCommandBackup = @"
                DECLARE @DBNAME      NVARCHAR(1000) = @P_DBNAME
                DECLARE @PATH        NVARCHAR(1000) = @P_PATH
                DECLARE @MEDIANAME   NVARCHAR(1000) = @P_MEDIANAME

                BACKUP LOG @DBNAME TO  DISK = @PATH WITH NAME = @MEDIANAME
                ";

            if (overwriteIfExists)
                sqlCommandBackup += ", INIT";

            if (checksum)
                sqlCommandBackup += ", CHECKSUM";

            if (continueOnError)
                sqlCommandBackup += ", CONTINUE_AFTER_ERROR";

            if (compression == UseCompressionEnum.CompressBackup)
                sqlCommandBackup += ", COMPRESSION";
            else if (compression == UseCompressionEnum.DoNotCompressBackup)
                sqlCommandBackup += ", NO_COMPRESSION";

            using SqlConnection cnt = new(connectionString);
            ManualResetEvent waitInfoMessage = new(false);
            cnt.InfoMessage += (sender, e) =>
            {
                if (e.Message.Contains("BACKUP WITH CONTINUE_AFTER_ERROR successfully"))
                    result = _backupSuccessChecksumError;
                else if (e.Message.Contains("BACKUP LOG successfully"))
                    result = _backupSuccess;

                waitInfoMessage.Set();
            };

            cnt.Open();

            using SqlCommand cmd = new(sqlCommandBackup, cnt);
            cmd.Parameters.Add("@P_DBNAME", SqlDbType.NVarChar).Value = databaseName;
            cmd.Parameters.Add("@P_PATH", SqlDbType.NVarChar).Value = fullPathDestination;
            cmd.Parameters.Add("@P_MEDIANAME", SqlDbType.NVarChar).Value = mediaName;
            cmd.ExecuteNonQuery();
            waitInfoMessage.WaitOne();
        }
        catch (Exception ex)
        {
            result = _backupError;
            logger?.Error(this, $"Backup of database {databaseName} failed", ex);
        }

        return result;
    }

    private bool VerifyBackup(string connectionString, string databaseName, string destination, string fileName, BackupTypeEnum backupType, IPluginInstanceLogger? logger)
    {
        bool result = false;

        try
        {
            string fullPathDestination;

            if (string.IsNullOrEmpty(fileName))
            {
                if (backupType == BackupTypeEnum.Full)
                    fullPathDestination = Path.Combine(destination, databaseName + ".bak");
                else
                    fullPathDestination = Path.Combine(destination, databaseName + ".trn");
            }
            else
            {
                fullPathDestination = Path.Combine(destination, fileName);
            }

            string sqlCommandVerifyBackup = @"
                DECLARE @P_BACKUPSET_ID   INT

                SELECT @P_BACKUPSET_ID = position FROM msdb..backupset WHERE database_name=@P_DBNAME 
                                                        AND backup_set_id=(SELECT MAX(backup_set_id) FROM msdb..backupset WHERE database_name=@P_DBNAME )
                
                IF @P_BACKUPSET_ID IS NULL BEGIN RAISERROR(N'Verify failed. Backup information not found.', 16, 1) END
                
                RESTORE VERIFYONLY FROM  DISK = @P_PATH WITH FILE = @P_BACKUPSET_ID,  NOUNLOAD,  NOREWIND
                IF @@ERROR <> 0 BEGIN RAISERROR(N'Verify failed. RESTORE command issued an error.', 16, 1) END
            ";

            using SqlConnection cnt = new(connectionString);
            ManualResetEvent WaitInfoMessage = new(false);
            cnt.InfoMessage += (sender, e) =>
            {
                if (e.Message.Contains("is valid."))
                    result = true;

                WaitInfoMessage.Set();
            };


            cnt.Open();

            using SqlCommand cmd = new(sqlCommandVerifyBackup, cnt);
            cmd.Parameters.Add("@P_DBNAME", SqlDbType.NVarChar).Value = databaseName;
            cmd.Parameters.Add("@P_PATH", SqlDbType.NVarChar).Value = fullPathDestination;
            cmd.ExecuteNonQuery();
            WaitInfoMessage.WaitOne();
        }
        catch (Exception ex)
        {
            result = false;
            logger?.Error(this, $"Verification of database {databaseName} failed", ex);
        }

        return result;
    }

    protected override void RunIteration(int currentIteration)
    {
        _successfulBackupsNumber = 0;
        _failedBackupsNumber = 0;

        SqlServerBackupTaskConfig tConfig = (SqlServerBackupTaskConfig)_iterationConfig;

        string connectionString = $"Server={tConfig.Server};User ID={tConfig.Username};Password={tConfig.Password};{tConfig.ConnectionStringOptions}";

        bool getUserDatabases = tConfig.DatabasesToBackup == DatabasesToBackupEnum.AllUserDatabases;
        List<SqlServerDatabaseListItem>? currentDbList = SqlServer.GetDatabaseList(tConfig.Server, tConfig.Username, tConfig.Password, tConfig.ConnectionStringOptions, getUserDatabases);
        if (currentDbList == null)
        {
            _instanceLogger?.Error(this, "An error occurred while obtaining database list, cannot continue.");
            return;
        }

        List<string> dbToBackupList = [.. currentDbList.Select(t => t.Name)];

        if (tConfig.DatabasesToBackup == DatabasesToBackupEnum.SelectedDatabases)
        {
            dbToBackupList = [.. dbToBackupList.Where(t => tConfig.SelectedDatabases.Contains(t))];
        }

        string prevFileNameTemplate = tConfig.FileNameTemplate;
        foreach (string dbName in dbToBackupList)
        {
            try
            {
                tConfig.FileNameTemplate = prevFileNameTemplate.Replace("{DatabaseName}", dbName);
                _instanceLogger?.Info(this, $"Backing up database '{dbName}'...");
                int backupResult;

                if (tConfig.BackupType == BackupTypeEnum.Full)
                {
                    backupResult = BackupDatabase(connectionString, dbName, tConfig.DestinationPath, tConfig.FileNameTemplate, tConfig.OverwriteIfExists,
                                                    tConfig.PerformChecksum, tConfig.ContinueOnError,
                                                    $"OSRobot-Backup-{DateTime.Now.Ticks}", tConfig.UseCompression, _instanceLogger);

                    if (backupResult == _backupSuccess)
                        _instanceLogger?.Info(this, $"Backup database '{dbName}' completed");
                    else if (backupResult == _backupSuccessChecksumError)
                        _instanceLogger?.Info(this, $"Backup database '{dbName}' completed, but checksum failed");
                    else
                        _instanceLogger?.Info(this, $"Backup database '{dbName}' failed");
                }
                else
                {
                    backupResult = BackupTransactionLog(connectionString, dbName, tConfig.DestinationPath, tConfig.FileNameTemplate, tConfig.OverwriteIfExists,
                                                        tConfig.PerformChecksum, tConfig.ContinueOnError,
                                                        $"OSRobot-TranLogBackup-{DateTime.Now.Ticks}", tConfig.UseCompression, _instanceLogger);

                    if (backupResult == _backupSuccess)
                        _instanceLogger?.Info(this, $"Backup transaction log '{dbName}' completed");
                    else if (backupResult == _backupSuccessChecksumError)
                        _instanceLogger?.Info(this, $"Backup transaction log '{dbName}' completed, but checksum failed");
                    else
                        _instanceLogger?.Info(this, $"Backup transaction log '{dbName}' failed");
                }

                if (backupResult == _backupSuccess)
                    _successfulBackupsNumber++;
                else
                    _failedBackupsNumber++;

                if (backupResult == _backupSuccess || backupResult == _backupSuccessChecksumError)
                {
                    if (tConfig.VerifyBackup)
                    {
                        _instanceLogger?.Info(this, $"Starting backup verification '{dbName}'...");

                        if (VerifyBackup(connectionString, dbName, tConfig.DestinationPath, tConfig.FileNameTemplate, tConfig.BackupType, _instanceLogger))
                            _instanceLogger?.Info(this, "Verification OK");
                        else
                            _instanceLogger?.Info(this, "Verification failed");
                    }
                }
            }
            catch (Exception ex)
            {
                _instanceLogger?.Error(this, $"Error backing up database '{dbName}'", ex);
            }
        }
    }

    protected override void PostIterationSucceded(int currentIteration, ExecResult result, DynamicDataSet dDataSet)
    {
        dDataSet.Add(SqlServerBackupTaskCommon.DynDataKeySuccessfulBackupsNumber, _successfulBackupsNumber);
        dDataSet.Add(SqlServerBackupTaskCommon.DynDataKeyFailedBackupsNumber, _failedBackupsNumber);
    }

    protected override void PostIterationFailed(int currentIteration, ExecResult result, DynamicDataSet dDataSet)
    {
        dDataSet.Add(SqlServerBackupTaskCommon.DynDataKeySuccessfulBackupsNumber, _successfulBackupsNumber);
        dDataSet.Add(SqlServerBackupTaskCommon.DynDataKeyFailedBackupsNumber, _failedBackupsNumber);
    }
}
