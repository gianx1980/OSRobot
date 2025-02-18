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

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging;
using System.Text;
using System.Text.RegularExpressions;


namespace OSRobot.Server.Plugins.FtpSftpTask;

public class FtpSftpTask : IterationTask
{
    private void BuildRemotePath(FtpSftpClient fileTransferClient, string remotePath, bool skipLastSegment)
    {
        List<string> pathItems = FtpSftpTaskCommon.SplitRemotePath(remotePath);

        if (pathItems.Count > 0)
        {
            StringBuilder fullPath = new StringBuilder();
            for (int i = 0; i < pathItems.Count - 1; i++)
            {
                string item = pathItems[i];
                if (!string.IsNullOrEmpty(item))
                {
                    fullPath.Append($"/{item}");
                    string fullPathString = fullPath.ToString();
                    if (!fileTransferClient.RemoteDirectoryExists(fullPathString))
                        fileTransferClient.RemoteCreateDirectory(fullPathString);
                }

                if (skipLastSegment && (i == (pathItems.Count - 1)))
                    break;
            }
        }
    }

    private void BuildLocalPath(FtpSftpClient fileTransferClient, string localPath, bool skipLastSegment)
    {
        List<string> pathItems = FtpSftpTaskCommon.SplitLocalPath(localPath);

        if (pathItems.Count > 0)
        {
            StringBuilder fullPath = new StringBuilder();
            for (int i = 0; i < pathItems.Count - 1; i++)
            {
                string item = pathItems[i];
                if (!string.IsNullOrEmpty(item))
                {
                    if (Regex.Match(item, @"[A-Z]:", RegexOptions.IgnoreCase).Success)
                    {
                        fullPath.Append($"{Path.DirectorySeparatorChar}{item}");
                    }
                    else
                    {
                        // TODO: Correct here to use RemoteDirectoryExists & RemoteCreateDirectory?
                        fullPath.Append($"{Path.DirectorySeparatorChar}{item}");
                        string fullPathString = fullPath.ToString();
                        if (!fileTransferClient.RemoteDirectoryExists(fullPathString))
                            fileTransferClient.RemoteCreateDirectory(fullPathString);
                    }
                }

                if (skipLastSegment && (i == (pathItems.Count - 1)))
                    break;
            }
        }
    }

    private void UploadFile(FtpSftpClient fileTransferClient, string localPath, string remotePath, bool overwriteFileIfExists, bool createDirectoryTree)
    {
        if (overwriteFileIfExists || !fileTransferClient.RemoteFileExists(remotePath))
        {
            if (createDirectoryTree)
                BuildRemotePath(fileTransferClient, remotePath, true);
                
            fileTransferClient.Upload(localPath, remotePath, overwriteFileIfExists);
        }
    }

    private void UploadDirectory(FtpSftpClient fileTransferClient, string localPath, string remotePath, bool overwriteFileIfExists, bool recursivelyCopyDirectories)
    {
        List<FtpSftpFileInfo> fileList = fileTransferClient.LocalListing(localPath);
        BuildRemotePath(fileTransferClient, remotePath, false);

        foreach (FtpSftpFileInfo fInfo in fileList)
        {
            if (!fInfo.IsDirectory)
            {
                UploadFile(fileTransferClient, Path.Combine(localPath, fInfo.FileName), FtpSftpTaskCommon.CombineRemotePath(remotePath, fInfo.FileName), overwriteFileIfExists, false);
            }
            else
            {
                if (recursivelyCopyDirectories)
                    UploadDirectory(fileTransferClient, Path.Combine(localPath, fInfo.FileName), FtpSftpTaskCommon.CombineRemotePath(remotePath, fInfo.FileName), overwriteFileIfExists, recursivelyCopyDirectories);
            }
        }
    }

    private void DownloadFile(FtpSftpClient fileTransferClient, string localPath, string remotePath, bool overwriteFileIfExists, bool createDirectoryTree)
    {
        if (overwriteFileIfExists || !fileTransferClient.LocalFileExists(localPath))
        {
            if (createDirectoryTree)
                BuildLocalPath(fileTransferClient, localPath, true);
            fileTransferClient.Download(localPath, remotePath);
        }
    }

    private void DownloadDirectory(FtpSftpClient fileTransferClient, string localPath, string remotePath, bool overwriteFileIfExists, bool recursivelyCopyDirectories)
    {
        List<FtpSftpFileInfo> fileList = fileTransferClient.LocalListing(remotePath);
        BuildLocalPath(fileTransferClient, remotePath, false);

        foreach (FtpSftpFileInfo fInfo in fileList)
        {
            if (!fInfo.IsDirectory)
            {
                DownloadFile(fileTransferClient, localPath, remotePath, overwriteFileIfExists, false);
            }
            else
            {
                if (recursivelyCopyDirectories)
                    DownloadDirectory(fileTransferClient, localPath, remotePath, overwriteFileIfExists, recursivelyCopyDirectories);
            }
        }
    }

    private void ManageCopyItem(FtpSftpClient fileTransferClient, FtpSftpCopyItem copyItem, IPluginInstanceLogger logger)
    {
        if (copyItem.LocalToRemote)
        {
            if (fileTransferClient.LocalIsDirectory(copyItem.LocalPath))
            {
                logger.Info($"Copying directory {copyItem.LocalPath} to {copyItem.RemotePath}...");
                UploadDirectory(fileTransferClient, copyItem.LocalPath, copyItem.RemotePath, copyItem.OverwriteFileIfExists, copyItem.RecursivelyCopyDirectories);
            }
            else
            {
                logger.Info($"Copying file {copyItem.LocalPath} to {copyItem.RemotePath}...");
                UploadFile(fileTransferClient, copyItem.LocalPath, copyItem.RemotePath, copyItem.OverwriteFileIfExists, true);
            }
        }
        else
        {
            if (fileTransferClient.RemoteIsDirectory(copyItem.LocalPath))
            {
                logger.Info($"Copying directory {copyItem.RemotePath} to {copyItem.LocalPath}...");
                DownloadDirectory(fileTransferClient, copyItem.LocalPath, copyItem.RemotePath, copyItem.OverwriteFileIfExists, copyItem.RecursivelyCopyDirectories);
            }
            else
            {
                logger.Info($"Copying file {copyItem.RemotePath} to {copyItem.LocalPath}...");
                DownloadFile(fileTransferClient, copyItem.LocalPath, copyItem.RemotePath, copyItem.OverwriteFileIfExists, true);
            }
        }
    }

    private void ManageDeleteItem(FtpSftpClient fileTransferClient, FtpSftpDeleteItem deleteItem, IPluginInstanceLogger logger)
    {
        if (fileTransferClient.RemoteDirectoryExists(deleteItem.RemotePath) 
            || fileTransferClient.RemoteFileExists(deleteItem.RemotePath))
        {
            if (fileTransferClient.RemoteIsDirectory(deleteItem.RemotePath))
            {
                logger.Info($"Deleting directory {deleteItem.RemotePath}...");
                fileTransferClient.RemoteDirectoryDelete(deleteItem.RemotePath);
            }
            else
            {
                logger.Info($"Deleting file {deleteItem.RemotePath}...");
                fileTransferClient.RemoteFileDelete(deleteItem.RemotePath);
            }
        }
    }

    protected override void RunIteration(int currentIteration)
    {
        FtpSftpTaskConfig tConfig = (FtpSftpTaskConfig)_iterationConfig;

        using (FtpSftpClient fileTransferClient = new FtpSftpClient())
        {
            _instanceLogger?.Info($"Connecting to host: {tConfig.Host} Port: {tConfig.Port} Username: {tConfig.Username}");
            fileTransferClient.Connect(tConfig.Protocol, tConfig.Host, int.Parse(tConfig.Port), tConfig.Username, tConfig.Password);
            _instanceLogger?.Info("Connection established");

            if (tConfig.Command == CommandEnum.Copy)
            {
                _instanceLogger?.Info("Starting copy files...");

                foreach (FtpSftpCopyItem copyItem in tConfig.CopyItems)
                {
                    FtpSftpCopyItem? copyItemCopy = (FtpSftpCopyItem?)CoreHelpers.CloneObjects(copyItem);
                    if (copyItemCopy == null)
                        throw new ApplicationException("Cloning CopyItem returned null");

                    copyItemCopy.LocalPath = DynamicDataParser.ReplaceDynamicData(copyItemCopy.LocalPath, _dataChain, currentIteration);
                    copyItemCopy.RemotePath = DynamicDataParser.ReplaceDynamicData(copyItemCopy.RemotePath, _dataChain, currentIteration);
                    ManageCopyItem(fileTransferClient, copyItemCopy, _instanceLogger!);
                }

                _instanceLogger?.Info("Copy files completed");
            }
            else
            {
                _instanceLogger?.Info("Starting delete files...");

                foreach (FtpSftpDeleteItem deleteItem in tConfig.DeleteItems)
                {
                    FtpSftpDeleteItem? deleteItemCopy = (FtpSftpDeleteItem?)CoreHelpers.CloneObjects(deleteItem);
                    if (deleteItemCopy == null)
                        throw new ApplicationException("Cloning DeleteItem returned null");

                    deleteItemCopy.RemotePath = DynamicDataParser.ReplaceDynamicData(deleteItemCopy.RemotePath, _dataChain, currentIteration);
                    ManageDeleteItem(fileTransferClient, deleteItemCopy, _instanceLogger!);
                }

                _instanceLogger?.Info("Delete files completed");
            }
        }
    }
}
