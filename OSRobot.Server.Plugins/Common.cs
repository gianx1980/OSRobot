// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core.Data;

namespace OSRobot.Server.Plugins;

static class Common
{
    public static string GetUniqueFileName(string filePathName)
    {
        if (!File.Exists(filePathName))
            return filePathName;

        FileInfo FI = new(filePathName);

        string NewFileName;
        int Attempt = 1;

        do
        {
            NewFileName = $"{FI.Directory!.FullName}\\{Path.GetFileNameWithoutExtension(FI.Name)}-{Attempt}{FI.Extension}";
            Attempt++;

        } while (File.Exists(NewFileName));

        return NewFileName;
    }

    public static int? GetNullableInt(string val)
    {
        if (DataValidationHelper.IsEmptyString(val))
            return null;

        return int.Parse(val);
    }

    public static string GetStringFromNullable(int? val)
    {
        if (val == null)
            return string.Empty;
        return val.ToString()!;
    }

    public static bool IsDirectory(string path)
    {
        FileAttributes Attr = File.GetAttributes(path);
        return Attr.HasFlag(FileAttributes.Directory);
    }
}
