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

namespace OSRobot.Server.Plugins.UnzipTask;

public class UnzipTask : MultipleIterationTask
{
    private bool UncompressArchive(string zipFileName, string outputFolder, IfDestFileExistsType ifDestFileExists)
    {
        using FileStream fs = File.OpenRead(zipFileName);
        using ZipFile zipFileToExtract = new(fs);
        foreach (ZipEntry zipItem in zipFileToExtract)
        {
            if (!zipItem.IsFile)
            {
                // Ignore directories
                continue;
            }

            string EntryFileName = zipItem.Name;

            byte[] buffer = new byte[4096];
            using Stream ZipStream = zipFileToExtract.GetInputStream(zipItem);
            string fullZipToPath = Path.Combine(outputFolder, EntryFileName);
            string directoryName = Path.GetDirectoryName(fullZipToPath) ?? string.Empty;

            if (directoryName.Length > 0)
            {
                Directory.CreateDirectory(directoryName);
            }


            if (File.Exists(fullZipToPath))
            {
                if (ifDestFileExists == IfDestFileExistsType.Fail)
                    return false;
                else if (ifDestFileExists == IfDestFileExistsType.CreateWithUniqueNames)
                    fullZipToPath = Common.GetUniqueFileName(fullZipToPath);
            }

            using FileStream streamWriter = File.Create(fullZipToPath);
            StreamUtils.Copy(ZipStream, streamWriter, buffer);
        }

        return true;
    }

    protected override void RunMultipleIterationTask(int currentIteration)
    {
        UnzipTaskConfig config = (UnzipTaskConfig)_iterationTaskConfig;
        
        _instanceLogger.Info(this, $"Uncompressing archive {config.Source} to {config.Destination}...");
        bool completed = UncompressArchive(config.Source, config.Destination, config.IfDestFileExists);

        if (!completed)
            throw new ApplicationException("One or more files with the same name found in destination folder.");
    }
}
