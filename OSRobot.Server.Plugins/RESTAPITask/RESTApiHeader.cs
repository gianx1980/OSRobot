// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Plugins.RESTApiTask;

public class RESTApiHeader
{
    public string Name { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"{Name}:{Value}";
    }
}
