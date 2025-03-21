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
