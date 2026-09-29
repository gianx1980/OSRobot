// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Plugins.FileSystemTask;

public class FileSystemTaskDeleteItem
{
    public string DeletePath { get; set; } = string.Empty;
    public string FilesOlderThanDays { get; set; } = string.Empty;
    public string FilesOlderThanHours { get; set; } = string.Empty;
    public string FilesOlderThanMinutes { get; set; } = string.Empty;
    public bool RecursivelyDelete { get; set; }

    public override string ToString()
    {
        return $"{Resource.TxtPath}: {DeletePath}";
    }
}
