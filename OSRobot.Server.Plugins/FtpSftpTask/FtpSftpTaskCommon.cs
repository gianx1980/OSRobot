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

using System.Text.RegularExpressions;

namespace OSRobot.Server.Plugins.FtpSftpTask;

internal static class FtpSftpTaskCommon
{
    public static List<string> SplitRemotePath(string remotePath)
    {
        List<string> result = [];
        string[] items_1 = Regex.Split(remotePath, @"\\");

        foreach (string item_1 in items_1)
        {
            string[] items_2 = Regex.Split(item_1, @"/");
            foreach (string item_2 in items_2)
            {
                if (!string.IsNullOrEmpty(item_2))
                    result.Add(item_2);
            }
        }

        return result;
    }

    public static List<string> SplitLocalPath(string localPath)
    {
        List<string> result = [];
        string separator = Path.DirectorySeparatorChar.ToString();

        if (Path.DirectorySeparatorChar == '\\')
            separator += '\\';

        string[] items = Regex.Split(localPath, separator);
        foreach (string item in items)
        {
            if (!string.IsNullOrEmpty(item))
                result.Add(item);
        }

        return result;
    }

    public static string CombineRemotePath(params string[] paths)
    {
        return Path.Combine(paths).Replace('\\', '/');
    }
}
