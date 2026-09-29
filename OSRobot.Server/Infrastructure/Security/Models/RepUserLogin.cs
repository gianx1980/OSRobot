// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Infrastructure.Security.Models;

public class RepUserLogin
{
    public long? Id { get; set; }
    public string? Username { get; set; }
    public byte[]? Salt { get; set; }
    public byte[]? Password { get; set; }
}
