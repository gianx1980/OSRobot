// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Plugins.ExcelFileTask;

public class ExcelFileColumnDefinition
{
    public string HeaderTitle { get; set; } = string.Empty;
    public string CellValue { get; set; } = string.Empty;

    public override string ToString()
    {
        string Result = string.Empty;

        if (!string.IsNullOrEmpty(HeaderTitle))
            Result += HeaderTitle;
        else
            Result += Resource.TxtNoHeader;

        if (!string.IsNullOrEmpty(CellValue))
            Result += ": " + CellValue;
        else
            Result += ": " + Resource.TxtNoValue;

        return Result;
    }
}
