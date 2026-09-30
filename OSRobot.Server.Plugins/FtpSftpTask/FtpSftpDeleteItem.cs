// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Plugins.FtpSftpTask;

public class FtpSftpDeleteItem
{
    public string RemotePath { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"{Resource.TxtPath}: {RemotePath}";
    }
}
