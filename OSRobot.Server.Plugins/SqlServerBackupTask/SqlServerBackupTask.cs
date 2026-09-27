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
using OSRobot.Server.Plugins.Infrastructure.Utilities.SqlClient;
using System.Data;

namespace OSRobot.Server.Plugins.SqlServerBackupTask;

public class SqlServerBackupTask : MultipleIterationTask
{
    private const int _backupSuccess = -1;
    private const int _backupSuccessChecksumError = -2;
    private const int _backupError = -3;

    private int _successfulBackupsNumber = 0;
    private int _failedBackupsNumber = 0;

    private async Task<int> BackupDatabase(string connectionString, string databaseName, string destination, string fileName, bool overwriteIfExists,
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
            using ManualResetEvent WaitInfoMessage = new(false);
            Cnt.InfoMessage += (sender, e) =>
            {
                if (e.Message.Contains("BACKUP WITH CONTINUE_AFTER_ERROR successfully"))
                    Result = _backupSuccessChecksumError;
                else if (e.Message.Contains("BACKUP DATABASE successfully"))
                    Result = _backupSuccess;

                WaitInfoMessage.Set();
            };

            await Cnt.OpenAsync(_cancellationToken);

            using SqlCommand Cmd = new(SqlCommandBackup, Cnt);
            Cmd.Parameters.Add("@P_DBNAME", SqlDbType.NVarChar).Value = databaseName;
            Cmd.Parameters.Add("@P_PATH", SqlDbType.NVarChar).Value = FullPathDestination;
            Cmd.Parameters.Add("@P_MEDIANAME", SqlDbType.NVarChar).Value = mediaName;
            await Cmd.ExecuteNonQueryAsync(_cancellationToken);
            // InfoMessage fires synchronously as part of the command completing, so by the
            // time ExecuteNonQueryAsync above has finished this is already signaled (or
            // signals essentially immediately) - not a meaningful blocking wait.
            WaitInfoMessage.WaitOne();
        }
        catch (Exception ex)
        {
            Result = _backupError;
            logger?.Error(this, $"Backup of database {databaseName} failed", ex);
        }

        return Result;
    }

    private async Task<int> BackupTransactionLog(string connectionString, string databaseName, string destination, string fileName, bool overwriteIfExists,
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
            using ManualResetEvent waitInfoMessage = new(false);
            cnt.InfoMessage += (sender, e) =>
            {
                if (e.Message.Contains("BACKUP WITH CONTINUE_AFTER_ERROR successfully"))
                    result = _backupSuccessChecksumError;
                else if (e.Message.Contains("BACKUP LOG successfully"))
                    result = _backupSuccess;

                waitInfoMessage.Set();
            };

            await cnt.OpenAsync(_cancellationToken);

            using SqlCommand cmd = new(sqlCommandBackup, cnt);
            cmd.Parameters.Add("@P_DBNAME", SqlDbType.NVarChar).Value = databaseName;
            cmd.Parameters.Add("@P_PATH", SqlDbType.NVarChar).Value = fullPathDestination;
            cmd.Parameters.Add("@P_MEDIANAME", SqlDbType.NVarChar).Value = mediaName;
            await cmd.ExecuteNonQueryAsync(_cancellationToken);
            waitInfoMessage.WaitOne();
        }
        catch (Exception ex)
        {
            result = _backupError;
            logger?.Error(this, $"Backup of database {databaseName} failed", ex);
        }

        return result;
    }

    private async Task<bool> VerifyBackup(string connectionString, string databaseName, string destination, string fileName, BackupTypeEnum backupType, IPluginInstanceLogger? logger)
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
            using ManualResetEvent WaitInfoMessage = new(false);
            cnt.InfoMessage += (sender, e) =>
            {
                if (e.Message.Contains("is valid."))
                    result = true;

                WaitInfoMessage.Set();
            };


            await cnt.OpenAsync(_cancellationToken);

            using SqlCommand cmd = new(sqlCommandVerifyBackup, cnt);
            cmd.Parameters.Add("@P_DBNAME", SqlDbType.NVarChar).Value = databaseName;
            cmd.Parameters.Add("@P_PATH", SqlDbType.NVarChar).Value = fullPathDestination;
            await cmd.ExecuteNonQueryAsync(_cancellationToken);
            WaitInfoMessage.WaitOne();
        }
        catch (Exception ex)
        {
            result = false;
            logger?.Error(this, $"Verification of database {databaseName} failed", ex);
        }

        return result;
    }

    protected override async Task RunMultipleIterationTaskAsync(int currentIteration)
    {
        _successfulBackupsNumber = 0;
        _failedBackupsNumber = 0;

        SqlServerBackupTaskConfig config = (SqlServerBackupTaskConfig)_iterationTaskConfig;

        string connectionString = $"Server={config.Server};User ID={config.Username};Password={config.Password};{config.ConnectionStringOptions}";

        bool getUserDatabases = config.DatabasesToBackup == DatabasesToBackupEnum.AllUserDatabases;
        List<SqlServerDatabaseListItem>? currentDbList = SqlServer.GetDatabaseList(config.Server, config.Username, config.Password, config.ConnectionStringOptions, getUserDatabases, _instanceLogger);
        if (currentDbList == null)
        {
            // Must throw, not return: returning normally would make the base class record this
            // iteration as a success (see MultipleIterationTask.RunTaskAsync), so a backup that
            // never reached the server would look like it worked.
            throw new ApplicationException("An error occurred while obtaining database list, cannot continue.");
        }

        List<string> dbToBackupList = [.. currentDbList.Select(t => t.Name)];

        if (config.DatabasesToBackup == DatabasesToBackupEnum.SelectedDatabases)
        {
            dbToBackupList = [.. dbToBackupList.Where(t => config.SelectedDatabases.Contains(t))];
        }

        string prevFileNameTemplate = config.FileNameTemplate;
        foreach (string dbName in dbToBackupList)
        {
            try
            {
                config.FileNameTemplate = prevFileNameTemplate.Replace("{DatabaseName}", dbName);
                _instanceLogger?.Info(this, $"Backing up database '{dbName}'...");
                int backupResult;

                if (config.BackupType == BackupTypeEnum.Full)
                {
                    backupResult = await BackupDatabase(connectionString, dbName, config.DestinationPath, config.FileNameTemplate, config.OverwriteIfExists,
                                                    config.PerformChecksum, config.ContinueOnError,
                                                    $"OSRobot-Backup-{DateTime.Now.Ticks}", config.UseCompression, _instanceLogger);

                    if (backupResult == _backupSuccess)
                        _instanceLogger?.Info(this, $"Backup database '{dbName}' completed");
                    else if (backupResult == _backupSuccessChecksumError)
                        _instanceLogger?.Info(this, $"Backup database '{dbName}' completed, but checksum failed");
                    else
                        _instanceLogger?.Info(this, $"Backup database '{dbName}' failed");
                }
                else
                {
                    backupResult = await BackupTransactionLog(connectionString, dbName, config.DestinationPath, config.FileNameTemplate, config.OverwriteIfExists,
                                                        config.PerformChecksum, config.ContinueOnError,
                                                        $"OSRobot-TranLogBackup-{DateTime.Now.Ticks}", config.UseCompression, _instanceLogger);

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
                    if (config.VerifyBackup)
                    {
                        _instanceLogger?.Info(this, $"Starting backup verification '{dbName}'...");

                        if (await VerifyBackup(connectionString, dbName, config.DestinationPath, config.FileNameTemplate, config.BackupType, _instanceLogger))
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

    protected override void PostTaskSucceded(int currentIteration, ExecResult result, DynamicDataSet dDataSet)
    {
        dDataSet.TryAdd(SqlServerBackupTaskCommon.DynDataKeySuccessfulBackupsNumber, _successfulBackupsNumber);
        dDataSet.TryAdd(SqlServerBackupTaskCommon.DynDataKeyFailedBackupsNumber, _failedBackupsNumber);
    }

    protected override void PostTaskFailed(int currentIteration, ExecResult result, DynamicDataSet dDataSet)
    {
        dDataSet.TryAdd(SqlServerBackupTaskCommon.DynDataKeySuccessfulBackupsNumber, _successfulBackupsNumber);
        dDataSet.TryAdd(SqlServerBackupTaskCommon.DynDataKeyFailedBackupsNumber, _failedBackupsNumber);
    }
}
