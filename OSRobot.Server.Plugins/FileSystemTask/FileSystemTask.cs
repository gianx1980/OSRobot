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
using OSRobot.Server.Core.Data;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging.Abstract;


namespace OSRobot.Server.Plugins.FileSystemTask;

public class FileSystemTask : IterationTask
{
    private bool _filePathExists = false;

    private TimeSpan GetFileTimeThreshold(string filesOlderThanDays, string filesOlderThanHours, string filesOlderThanMinutes)
    {
        int Days = 0;
        if (!DataValidationHelper.IsEmptyString(filesOlderThanDays))
            Days = int.Parse(filesOlderThanDays);

        int Hours = 0;
        if (!DataValidationHelper.IsEmptyString(filesOlderThanHours))
            Hours = int.Parse(filesOlderThanHours);

        int Minutes = 0;
        if (!DataValidationHelper.IsEmptyString(filesOlderThanMinutes))
            Minutes = int.Parse(filesOlderThanMinutes);

        return new TimeSpan(Days, Hours, Minutes, 0);
    }

    private bool IsNameAPattern(string objectName)
    {
        return objectName.Contains('*') || objectName.Contains('?');
    } 

    private void Copy(string source, string destination, bool overwriteExistingFiles, bool recursive = false, TimeSpan? olderThan = null)
    {
        // Check input parameters
        if (string.IsNullOrEmpty(source))
            throw new ApplicationException("Copy: Source is empty.");

        if (string.IsNullOrEmpty(destination))
            throw new ApplicationException("Copy: Destination is empty.");

        string? sourcePath = Path.GetDirectoryName(source);
        if (string.IsNullOrEmpty(sourcePath))
            throw new ApplicationException($"Copy: Source {source} must contain path information.");

        string? destinationPath = Path.GetDirectoryName(destination);
        if (string.IsNullOrEmpty(destination))
            throw new ApplicationException($"Copy: Destination {destination} must contain path information.");

        string sourceName = Path.GetFileName(source);

        bool isSourceANamePattern = IsNameAPattern(sourceName);
        bool isSourceADirectory = Directory.Exists(source);

        // ------------------------------------------------------------------
        // Single file copy.
        // If destination is a folder, then use the same filename of source
        // as the destination file name.
        // ------------------------------------------------------------------
        if (!isSourceADirectory && !isSourceANamePattern)
        {
            if (Directory.Exists(destination))
                destination = Path.Combine(destination, sourceName);

            File.Copy(source, destination);
            return;
        }

        // ------------------------------------------------------------------
        // Folder copy or folder copy with a file name pattern
        // ------------------------------------------------------------------
        string? searchPattern;
        DirectoryInfo sourceDirInfo;
        if (isSourceADirectory)
        {
            searchPattern = "*";
            sourceDirInfo = new DirectoryInfo(source);
        }
        else
        {
            searchPattern = sourceName;
            sourcePath = Path.GetDirectoryName(source);
            sourceDirInfo = new DirectoryInfo(sourcePath!);
        }

        // Cache directories before we start copying
        DirectoryInfo[] subDirectories = sourceDirInfo.GetDirectories();

        // Create the destination directory
        Directory.CreateDirectory(destination);

        // Get the files in the source directory and copy to the destination directory
        foreach (FileInfo file in sourceDirInfo.GetFiles(searchPattern))
        {
            // Check file time if needed
            if (olderThan != null && file.LastWriteTime >= DateTime.Now.Subtract((TimeSpan)olderThan))
                continue;

            string targetFilePath = Path.Combine(destination, file.Name);

            if (!overwriteExistingFiles && File.Exists(targetFilePath))
                continue;

            file.CopyTo(targetFilePath, overwriteExistingFiles);
        }

        // If recursive and copying subdirectories, recursively call this method
        if (recursive)
        {
            foreach (DirectoryInfo subDir in subDirectories)
            {
                string newDestinationDir = Path.Combine(destination, subDir.Name);
                Copy(Path.Combine(subDir.FullName, searchPattern), newDestinationDir, overwriteExistingFiles, true, olderThan);
            }
        }
    }


    private void Delete(string source, bool recursive = false, TimeSpan? olderThan = null)
    {
        // Check input parameters
        if (string.IsNullOrEmpty(source))
            throw new ApplicationException("Copy: Source is empty.");

        string? sourcePath = Path.GetDirectoryName(source);
        if (string.IsNullOrEmpty(sourcePath))
            throw new ApplicationException($"Copy: Source {source} must contain path information.");

        string sourceName = Path.GetFileName(source);

        bool isSourceANamePattern = IsNameAPattern(sourceName);
        bool isSourceADirectory = Directory.Exists(source);


        // ------------------------------------------------------------------
        // Single file delete.
        // ------------------------------------------------------------------
        if (!isSourceADirectory && !isSourceANamePattern)
        {
            File.Delete(source);
            return;
        }

        // ------------------------------------------------------------------
        // Folder delete or folder delete with a file name pattern
        // ------------------------------------------------------------------

        // If source contains a pattern, all the files that respect the pattern
        // will be deleted, but containing empty folders will not be delete.
        // If source contains a directory name, then the directory and its content
        // will be completely deleted.


        string? searchPattern;
        DirectoryInfo sourceDirInfo;
        if (isSourceADirectory)
        {
            searchPattern = "*";
            sourceDirInfo = new DirectoryInfo(source);
        }
        else
        {
            searchPattern = sourceName;
            sourcePath = Path.GetDirectoryName(source);
            sourceDirInfo = new DirectoryInfo(sourcePath!);
        }

        // Cache directories before we start copying
        DirectoryInfo[] subDirectories = sourceDirInfo.GetDirectories();

        // Get the files in the source directory and delete
        foreach (FileInfo file in sourceDirInfo.GetFiles(searchPattern))
        {
            // Check file time if needed
            if (olderThan != null && file.LastWriteTime >= DateTime.Now.Subtract((TimeSpan)olderThan))
                continue;

            // Don't throw exception if file not exists
            if (file.Exists)
                file.Delete();
        }

        // If recursive delete subdirectories, recursively call this method
        if (recursive)
        {
            foreach (DirectoryInfo subDir in subDirectories)
            {
                string tempDirPathName = isSourceADirectory ? subDir.FullName : Path.Combine(subDir.FullName, searchPattern);
                Delete(tempDirPathName, true, olderThan);
            }
        }

        // If directory is empty, delete
        if (isSourceADirectory && sourceDirInfo.GetFiles().Length == 0)
            sourceDirInfo.Delete();
    }

    private void ManageCopyItem(FileSystemTaskCopyItem copyItem, IPluginInstanceLogger? logger)
    {
        TimeSpan fileTimeThreshold = GetFileTimeThreshold(copyItem.FilesOlderThanDays, copyItem.FilesOlderThanHours, copyItem.FilesOlderThanMinutes);

        logger?.Info(this, $"Copying {copyItem.SourcePath} to {copyItem.DestinationPath}...");
        Copy(copyItem.SourcePath, copyItem.DestinationPath, copyItem.OverwriteFileIfExists, copyItem.RecursivelyCopy, fileTimeThreshold);
        logger?.Info(this, $"Copy of {copyItem.SourcePath} to {copyItem.DestinationPath} completed");
    }

    private void ManageDeleteItem(FileSystemTaskDeleteItem deleteItem, IPluginInstanceLogger? logger)
    {
        TimeSpan fileTimeThreshold = GetFileTimeThreshold(deleteItem.FilesOlderThanDays, deleteItem.FilesOlderThanHours, deleteItem.FilesOlderThanMinutes);

        logger?.Info(this, $"Deleting {deleteItem.DeletePath}...");
        Delete(deleteItem.DeletePath, deleteItem.RecursivelyDelete, fileTimeThreshold);
        logger?.Info(this, $"Delete of {deleteItem.DeletePath} completed");
    }

    protected override void RunIteration(int currentIteration)
    {
        FileSystemTaskConfig tConfig = (FileSystemTaskConfig)_iterationConfig;

        if (tConfig.Command == FileSystemTaskCommandType.Copy)
        {
            _instanceLogger?.Info(this, "Starting copy files...");

            foreach (FileSystemTaskCopyItem copyItem in tConfig.CopyItems)
            {
                FileSystemTaskCopyItem? copyItemCopy = (FileSystemTaskCopyItem?)CoreHelpers.CloneObjects(copyItem) ?? throw new ApplicationException("Cloning configuration returned null");
                copyItemCopy.SourcePath = DynamicDataParser.ReplaceDynamicData(copyItemCopy.SourcePath, _dataChain, currentIteration);
                copyItemCopy.DestinationPath = DynamicDataParser.ReplaceDynamicData(copyItemCopy.DestinationPath, _dataChain, currentIteration);
                copyItemCopy.FilesOlderThanDays = DynamicDataParser.ReplaceDynamicData(copyItemCopy.FilesOlderThanDays, _dataChain, currentIteration);
                copyItemCopy.FilesOlderThanHours = DynamicDataParser.ReplaceDynamicData(copyItemCopy.FilesOlderThanHours, _dataChain, currentIteration);
                copyItemCopy.FilesOlderThanMinutes = DynamicDataParser.ReplaceDynamicData(copyItemCopy.FilesOlderThanMinutes, _dataChain, currentIteration);
                ManageCopyItem(copyItemCopy, _instanceLogger);
            }

            _instanceLogger?.Info(this, "Copy files completed");
        }
        else if (tConfig.Command == FileSystemTaskCommandType.Delete)
        {
            _instanceLogger?.Info(this, "Starting delete files...");

            foreach (FileSystemTaskDeleteItem deleteItem in tConfig.DeleteItems)
            {
                FileSystemTaskDeleteItem? deleteItemCopy = (FileSystemTaskDeleteItem?)CoreHelpers.CloneObjects(deleteItem) ?? throw new ApplicationException("Cloning configuration returned null");
                deleteItemCopy.DeletePath = DynamicDataParser.ReplaceDynamicData(deleteItemCopy.DeletePath, _dataChain, currentIteration);
                deleteItemCopy.FilesOlderThanDays = DynamicDataParser.ReplaceDynamicData(deleteItemCopy.FilesOlderThanDays, _dataChain, currentIteration);
                deleteItemCopy.FilesOlderThanHours = DynamicDataParser.ReplaceDynamicData(deleteItemCopy.FilesOlderThanHours, _dataChain, currentIteration);
                deleteItemCopy.FilesOlderThanMinutes = DynamicDataParser.ReplaceDynamicData(deleteItemCopy.FilesOlderThanMinutes, _dataChain, currentIteration);
                ManageDeleteItem(deleteItemCopy, _instanceLogger);
            }

            _instanceLogger?.Info(this, "Delete files completed");
        }
        else if (tConfig.Command == FileSystemTaskCommandType.CreateFolder)
        {
            _instanceLogger?.Info(this, "Starting check existence of file/directory...");

            Directory.CreateDirectory(tConfig.CreateFolderPath);

            _instanceLogger?.Info(this, "Create directory completed");
        }
        else
        {
            _instanceLogger?.Info(this, "Starting check existence of file/directory...");
            _filePathExists = false;

            if (File.Exists(tConfig.CheckExistenceFilePath) || Directory.Exists(tConfig.CheckExistenceFilePath))
            {
                _filePathExists = true;
                _instanceLogger?.Info(this, $"File/Folder: {tConfig.CheckExistenceFilePath} exists");
            }

            _instanceLogger?.Info(this, "Check existence completed");
        }
    }

    protected override void PostIterationSucceded(int currentIteration, ExecResult result, DynamicDataSet dDataSet)
    {
        FileSystemTaskConfig tConfig = (FileSystemTaskConfig)_iterationConfig;

        if (tConfig.Command == FileSystemTaskCommandType.CheckExistence)
        {
            if (_filePathExists)
                dDataSet.Add("FilePathExists", 1);
            else
                dDataSet.Add("FilePathExists", 0);
        }
    }
}
