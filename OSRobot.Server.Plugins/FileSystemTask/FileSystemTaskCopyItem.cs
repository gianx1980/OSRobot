// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Plugins.FileSystemTask;

public class FileSystemTaskCopyItem
{
    public string SourcePath { get; set; } = string.Empty;

    public string DestinationPath { get; set; } = string.Empty;

    public string FilesOlderThanDays { get; set; } = string.Empty;
    public string FilesOlderThanHours { get; set; } = string.Empty;     
    public string FilesOlderThanMinutes { get; set; } = string.Empty;

    public bool OverwriteFileIfExists { get; set; }

    public bool RecursivelyCopy { get; set; }

    public override string ToString()
    {
        string result = $"{Resource.TxtFrom}: {SourcePath} {Resource.TxtTo}: {DestinationPath}";

        return $"{result} {(OverwriteFileIfExists ? Resource.TxtOverwriteIfExists : string.Empty)} {(RecursivelyCopy ? Resource.TxtRecursiveCopy : string.Empty)}";
    }
}
