// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Plugins.WriteTextFileTask;

public class WriteTextFileColumnDefinition
{
    public string HeaderTitle { get; set; } = string.Empty;
    public string FieldValue { get; set; } = string.Empty;
    public string FieldWidth { get; set; } = string.Empty;

    public override string ToString()
    {
        string result = string.Empty;

        if (!string.IsNullOrEmpty(HeaderTitle))
            result += HeaderTitle;
        else
            result += Resource.TxtNoHeader;

        if (!string.IsNullOrEmpty(FieldValue))
            result += ": " + FieldValue;
        else
            result += ": " + Resource.TxtNoValue;

        return result;
    }
}
