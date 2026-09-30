// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Models.DTO.Robot;

public class PluginDynDataSampleListItem(string name, string exampleValue, string internalName)
{
    public string Name { get; } = name;
    public string ExampleValue { get; } = exampleValue;
    public string InternalName { get; } = internalName;
}
