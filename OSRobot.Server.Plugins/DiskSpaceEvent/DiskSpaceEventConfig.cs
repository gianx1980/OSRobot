// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;

namespace OSRobot.Server.Plugins.DiskSpaceEvent;

public class DiskSpaceEventConfig : IEventConfig
{
    private const int _defaultCheckIntervalSeconds = 240;

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public bool Log { get; set; } = true;

    public List<DiskThreshold> DiskThresholds { get; set; } = [];

    public int CheckIntervalSeconds { get; set; } = _defaultCheckIntervalSeconds;
}
