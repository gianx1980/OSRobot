// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using ICSharpCode.SharpZipLib.Zip;
using OSRobot.Server.Core;

namespace OSRobot.Server.Plugins.UnzipTask;

public class UnzipTask : MultipleIterationTask
{
    private async Task<bool> UncompressArchive(string zipFileName, string outputFolder, IfDestFileExistsType ifDestFileExists)
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

            // SharpZipLib has no async copy helper, but both streams are plain BCL Streams,
            // so CopyToAsync gives a genuinely non-blocking bulk copy here.
            using FileStream streamWriter = File.Create(fullZipToPath);
            await ZipStream.CopyToAsync(streamWriter, _cancellationToken);
        }

        return true;
    }

    protected override async Task RunMultipleIterationTaskAsync(int currentIteration)
    {
        UnzipTaskConfig config = (UnzipTaskConfig)_iterationTaskConfig;

        _instanceLogger.Info(this, $"Uncompressing archive {config.Source} to {config.Destination}...");
        bool completed = await UncompressArchive(config.Source, config.Destination, config.IfDestFileExists);

        if (!completed)
            throw new ApplicationException("One or more files with the same name found in destination folder.");
    }
}
