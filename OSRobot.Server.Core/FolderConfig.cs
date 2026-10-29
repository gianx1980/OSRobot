// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Core;

public class FolderConfig : IPluginInstanceConfig
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// A disabled folder's events, and those of all its subfolders, are not started. Read from the folder's
    /// node in its parent folder (missing means enabled, as in jobs saved before folders could be disabled).
    /// </summary>
    public bool Enabled { get; set; } = true;
    public bool Log { get; set; }
}
