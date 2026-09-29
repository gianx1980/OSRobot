// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Plugins.FtpSftpTask;

public class FtpSftpFileInfo(string fileName, string fullPath, bool isFile, bool isDirectory, bool isLink)
{
    public string FileName { get; } = fileName;
    public string FullPath { get; } = fullPath;
    public bool IsFile { get; } = isFile;
    public bool IsDirectory { get; } = isDirectory;
    public bool IsLink { get; } = isLink;
}
