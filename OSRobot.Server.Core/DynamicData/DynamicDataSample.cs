// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Core.DynamicData;

public class DynamicDataSample(string internalName, string description, string example, bool isRecordset = false)
{
    public string InternalName { get; private set; } = internalName;
    public string Description { get; private set; } = description;
    public string Example { get; private set; } = example;
    public bool IsRecordset { get; private set; } = isRecordset;

    public override string ToString()
    {
        return $"{Description} ({InternalName})"; 
    }
}
