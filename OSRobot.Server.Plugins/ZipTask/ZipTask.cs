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

using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using OSRobot.Server.Core;

namespace OSRobot.Server.Plugins.ZipTask;

public class ZipTask : IterationTask
{

    private int ToNumericCompressionLevel(CompressionLevelType compressionLevel)
    {
        if (compressionLevel == CompressionLevelType.Low)
            return 1;
        else if (compressionLevel == CompressionLevelType.Medium)
            return 4;
        else // High
            return 9;
    }

    private void CompressFolder(string path, ZipOutputStream zipStream, int folderOffset, bool skipEmptyFolder)
    {
        string[] files = Directory.GetFiles(path);
        foreach (string fileName in files)
        {
            CompressFile(fileName, zipStream, folderOffset);
        }

        // Recursively call CompressFolder on all folders in path
        string[] folders = Directory.GetDirectories(path);
        foreach (string folder in folders)
        {
            CompressFolder(folder, zipStream, folderOffset, skipEmptyFolder);
        }

        if (files.Length == 0 && folders.Length == 0 && !skipEmptyFolder)
        {
            DirectoryInfo fi = new(path);
            string EntryName = path.Substring(folderOffset);
            EntryName = ZipEntry.CleanName(EntryName + "/empty.txt");   // It seems the only way to mantain an empty folder...
            ZipEntry NewEntry = new(EntryName)
            {
                DateTime = fi.LastWriteTime
            };
            zipStream.PutNextEntry(NewEntry);
            zipStream.CloseEntry();
        }
    }


    private void CompressFile(string filePathName, ZipOutputStream zipStream, int folderOffset)
    {
        FileInfo fi = new(filePathName);

        // Make the name in zip based on the folder
        string entryName = filePathName.Substring(folderOffset);

        // Remove drive from name and fix slash direction
        entryName = ZipEntry.CleanName(entryName);

        ZipEntry newEntry = new(entryName)
        {
            // Note the zip format stores 2 second granularity
            DateTime = fi.LastWriteTime,

            // Specifying the AESKeySize triggers AES encryption. 
            // Allowable values are 0 (off), 128 or 256.
            // A password on the ZipOutputStream is required if using AES.
            //   newEntry.AESKeySize = 256;

            // To permit the zip to be unpacked by built-in extractor in WinXP and Server2003,
            // WinZip 8, Java, and other older code, you need to do one of the following: 
            // Specify UseZip64.Off, or set the Size.
            // If the file may be bigger than 4GB, or you do not need WinXP built-in compatibility, 
            // you do not need either, but the zip will be in Zip64 format which
            // not all utilities can understand.
            //   zipStream.UseZip64 = UseZip64.Off;
            Size = fi.Length
        };

        zipStream.PutNextEntry(newEntry);

        // Zip the file in buffered chunks
        // the "using" will close the stream even if an exception occurs
        var buffer = new byte[4096];
        using (FileStream fsInput = File.OpenRead(filePathName))
        {
            StreamUtils.Copy(fsInput, zipStream, buffer);
        }
        zipStream.CloseEntry();
    }

    private void CompressItem(string itemPathName, string zipFileName, bool includeSubFolders, bool storeFullPath, bool skipEmptyFolder, int compressionLevel)
    {
        using FileStream fsOut = File.Create(zipFileName);
        using ZipOutputStream zipStream = new(fsOut);

        //0-9, 9 being the highest level of compression
        zipStream.SetLevel(compressionLevel);

        string itemName = Path.GetFileName(itemPathName);       // It might contain a pattern!
        string itemFolderName = Path.GetDirectoryName(itemPathName) ?? string.Empty;

        // This setting will strip the leading part of the folder path in the entries, 
        // to make the entries relative to the starting folder.
        // To include the full path for each entry up to the drive root, assign to 0.
        int folderOffset = storeFullPath ? 0 : (itemFolderName.Length + (itemFolderName.EndsWith("\\") ? 0 : 1));

        string[] files = Directory.GetFiles(itemFolderName, itemName);
        foreach (string fileName in files)
        {
            CompressFile(fileName, zipStream, folderOffset);
        }

        if (includeSubFolders)
        {
            string[] folders = Directory.GetDirectories(itemFolderName, itemName);
            foreach (string folderName in folders)
            {
                CompressFolder(folderName, zipStream, folderOffset, skipEmptyFolder);
            }
        }
    }

    protected override void RunIteration(int currentIteration)
    {
        ZipTaskConfig tConfig = (ZipTaskConfig)_iterationConfig;
        
        if (File.Exists(tConfig.Destination))
        {
            if (tConfig.IfArchiveExists == IfArchiveExistsType.CreateWithUniqueNames)
            {
                tConfig.Destination = Common.GetUniqueFileName(tConfig.Destination);
            }
            else if (tConfig.IfArchiveExists == IfArchiveExistsType.Fail)
            {
                if (Config.Log)
                    _instanceLogger?.Error($"File name {tConfig.Destination} already exists.");

                throw new ApplicationException($"File name {tConfig.Destination} already exists.");
            }
        }

        _instanceLogger?.Info(this, $"Compressing {tConfig.Source} to {tConfig.Destination}...");
        CompressItem(tConfig.Source, tConfig.Destination, tConfig.IncludeFilesInSubFolders,
                        tConfig.StoreFullPath, tConfig.SkipEmptyFolders, ToNumericCompressionLevel(tConfig.CompressionLevel));
    }
}
