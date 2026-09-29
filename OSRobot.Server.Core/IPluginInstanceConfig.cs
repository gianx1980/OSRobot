// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Core;

public interface IPluginInstanceConfig
{
    int Id { get; set; }
    string Name { get; set; }
    bool Enabled { get; set; }
    bool Log { get; set; }
}
