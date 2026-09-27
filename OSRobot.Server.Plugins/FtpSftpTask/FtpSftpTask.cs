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
using OSRobot.Server.Core.Logging.Abstract;
using OSRobot.Server.Plugins.Infrastructure.Network;
using System.Text;

namespace OSRobot.Server.Plugins.FtpSftpTask;

public class FtpSftpTask : MultipleIterationTask
{
    private void BuildRemotePath(IFileTransferClient fileTransferClient, string remotePath, bool skipLastSegment)
    {
        List<string> pathItems = FtpSftpTaskCommon.SplitRemotePath(remotePath);

        if (pathItems.Count > 0)
        {
            if (skipLastSegment)
                pathItems.RemoveAt(pathItems.Count - 1);
            
            StringBuilder fullPath = new();
            for (int i = 0; i < pathItems.Count; i++)
            {
                string item = pathItems[i];
                if (!string.IsNullOrEmpty(item))
                {
                    fullPath.Append($"/{item}");
                    string fullPathString = fullPath.ToString();
                    if (!fileTransferClient.RemoteDirectoryExists(fullPathString))
                        fileTransferClient.RemoteCreateDirectory(fullPathString);
                }
            }
        }
    }

    private static void BuildLocalPath(string localPath, bool skipLastSegment)
    {
        string? directory = skipLastSegment ? Path.GetDirectoryName(localPath) : localPath;

        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
    }

    private void UploadFile(IFileTransferClient fileTransferClient, string localPath, string remotePath, bool overwriteFileIfExists, bool createDirectoryTree)
    {
        if (overwriteFileIfExists || !fileTransferClient.RemoteFileExists(remotePath))
        {
            if (createDirectoryTree)
                BuildRemotePath(fileTransferClient, remotePath, true);
                
            fileTransferClient.Upload(localPath, remotePath, overwriteFileIfExists);
        }
    }

    private void UploadDirectory(IFileTransferClient fileTransferClient, string localPath, string remotePath, bool overwriteFileIfExists, bool recursivelyCopyDirectories)
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

    private void DownloadFile(IFileTransferClient fileTransferClient, string localPath, string remotePath, bool overwriteFileIfExists, bool createDirectoryTree)
    {
        if (overwriteFileIfExists || !fileTransferClient.LocalFileExists(localPath))
        {
            if (createDirectoryTree)
                BuildLocalPath(localPath, true);
            fileTransferClient.Download(localPath, remotePath);
        }
    }

    private void DownloadDirectory(IFileTransferClient fileTransferClient, string localPath, string remotePath, bool overwriteFileIfExists, bool recursivelyCopyDirectories)
    {
        List<FtpSftpFileInfo> fileList = fileTransferClient.RemoteListing(remotePath);
        BuildLocalPath(localPath, false);

        foreach (FtpSftpFileInfo fInfo in fileList)
        {
            // SFTP listings include the "." and ".." entries.
            if (fInfo.FileName is "." or "..")
                continue;

            string itemLocalPath = Path.Combine(localPath, fInfo.FileName);
            string itemRemotePath = FtpSftpTaskCommon.CombineRemotePath(remotePath, fInfo.FileName);

            if (!fInfo.IsDirectory)
            {
                DownloadFile(fileTransferClient, itemLocalPath, itemRemotePath, overwriteFileIfExists, false);
            }
            else
            {
                if (recursivelyCopyDirectories)
                    DownloadDirectory(fileTransferClient, itemLocalPath, itemRemotePath, overwriteFileIfExists, recursivelyCopyDirectories);
            }
        }
    }

    private void ManageCopyItem(IFileTransferClient fileTransferClient, FtpSftpCopyItem copyItem, IPluginInstanceLogger logger)
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
            if (fileTransferClient.RemoteIsDirectory(copyItem.RemotePath))
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

    private void ManageDeleteItem(IFileTransferClient fileTransferClient, FtpSftpDeleteItem deleteItem, IPluginInstanceLogger logger)
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

    protected override Task RunMultipleIterationTaskAsync(int currentIteration)
    {
        FtpSftpTaskConfig config = (FtpSftpTaskConfig)_iterationTaskConfig;

        using IFileTransferClient fileTransferClient = PluginServices.CreateFileTransferClient();
        _instanceLogger?.Info($"Connecting to host: {config.Host} Port: {config.Port} Username: {config.Username}");
        fileTransferClient.Connect(config.Protocol, config.Host, int.Parse(config.Port), config.Username, config.Password);
        _instanceLogger?.Info("Connection established");

        if (config.Command == CommandEnum.Copy)
        {
            _instanceLogger?.Info("Starting copy files...");

            foreach (FtpSftpCopyItem copyItem in config.CopyItems)
            {
                FtpSftpCopyItem? copyItemCopy = (FtpSftpCopyItem?)CoreHelpers.CloneObjects(copyItem) ?? throw new ApplicationException("Cloning CopyItem returned null");
                copyItemCopy.LocalPath = DynamicDataParser.ReplaceDynamicData(copyItemCopy.LocalPath, _dataChain, currentIteration, _subInstanceIndex);
                copyItemCopy.RemotePath = DynamicDataParser.ReplaceDynamicData(copyItemCopy.RemotePath, _dataChain, currentIteration, _subInstanceIndex);
                ManageCopyItem(fileTransferClient, copyItemCopy, _instanceLogger!);
            }

            _instanceLogger?.Info("Copy files completed");
        }
        else
        {
            _instanceLogger?.Info("Starting delete files...");

            foreach (FtpSftpDeleteItem deleteItem in config.DeleteItems)
            {
                FtpSftpDeleteItem? deleteItemCopy = (FtpSftpDeleteItem?)CoreHelpers.CloneObjects(deleteItem) ?? throw new ApplicationException("Cloning DeleteItem returned null");
                deleteItemCopy.RemotePath = DynamicDataParser.ReplaceDynamicData(deleteItemCopy.RemotePath, _dataChain, currentIteration, _subInstanceIndex);
                ManageDeleteItem(fileTransferClient, deleteItemCopy, _instanceLogger!);
            }

            _instanceLogger?.Info("Delete files completed");
        }

        return Task.CompletedTask;
    }
}
