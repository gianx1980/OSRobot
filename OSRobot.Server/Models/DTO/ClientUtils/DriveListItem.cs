// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Models.DTO.ClientUtils;

public class DriveListItem(string name)
{
    public string Name { get; set; } = name;
}
