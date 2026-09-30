// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.RegularExpressions;

namespace OSRobot.Server.Plugins.FtpSftpTask;

internal static partial class FtpSftpTaskCommon
{
    public static List<string> SplitRemotePath(string remotePath)
    {
        List<string> result = [];
        string[] items_1 = SplitByBackslashRegex().Split(remotePath);

        foreach (string item_1 in items_1)
        {
            string[] items_2 = SplitBySlashRegex().Split(item_1);
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

    [GeneratedRegex(@"\\")]
    private static partial Regex SplitByBackslashRegex();
    [GeneratedRegex(@"/")]
    private static partial Regex SplitBySlashRegex();
}
