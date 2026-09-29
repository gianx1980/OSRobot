// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Models.DTO.ClientUtils;

public class DatabaseListItem(int id, string name)
{
    public int Id { get; set; } = id;
    public string Name { get; set; } = name;
}
