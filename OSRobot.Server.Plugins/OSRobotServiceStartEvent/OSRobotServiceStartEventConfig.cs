// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;

namespace OSRobot.Server.Plugins.OSRobotServiceStartEvent;
public class OSRobotServiceStartEventConfig : IEventConfig
{
    private const int _DefaultMinutesWithin = 10;

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public bool Log { get; set; } = true;

    public int? MinutesWithin { get; set; } = _DefaultMinutesWithin;
    public int? MinutesAfter { get; set; }
}
