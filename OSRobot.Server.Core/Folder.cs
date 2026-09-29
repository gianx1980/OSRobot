// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Collections;

namespace OSRobot.Server.Core;

public class Folder : IFolder
{
    public List<IPluginInstanceBase> Items { get; set; } = [];

    public IFolder? ParentFolder { get; set; } 

    #pragma warning disable CS8618
    public IPluginInstanceConfig Config { get; set; }
    #pragma warning restore CS8618

    public void Add(IPluginInstanceBase item)
    {
        Items.Add(item);
    }

    public IEnumerator<IPluginInstanceBase> GetEnumerator()
    {
        return Items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return Items.GetEnumerator();
    }

    public string GetPhysicalFullPath()
    {
        IFolder? folder = this;
        string fullPath = string.Empty;

        while (folder != null)
        {
            fullPath = folder.Config.Id.ToString() + Path.DirectorySeparatorChar + fullPath;
            folder = folder.ParentFolder;
        }

        return fullPath;
    }
}
