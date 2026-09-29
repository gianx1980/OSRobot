// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Plugins.FtpSftpTask;


public class FtpSftpCopyItem
{
    public bool LocalToRemote { get; set; }

    public string LocalPath { get; set; } = string.Empty;
    
    public string RemotePath { get; set; } = string.Empty;

    public bool OverwriteFileIfExists { get; set; }

    public bool RecursivelyCopyDirectories { get; set; }

    public override string ToString()
    {
        string Result = LocalToRemote ? $"{Resource.TxtFrom}: {LocalPath} {Resource.TxtTo}: {RemotePath}" : $"{Resource.TxtFrom}: {RemotePath} {Resource.TxtTo}: {LocalPath}";

        return $"{Result} {(OverwriteFileIfExists ? Resource.TxtOverwriteIfExists : string.Empty)} {(RecursivelyCopyDirectories ? Resource.TxtRecursiveCopy : string.Empty)}";
    }
}
