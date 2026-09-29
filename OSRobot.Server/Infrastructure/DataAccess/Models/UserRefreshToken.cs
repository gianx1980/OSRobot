// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Infrastructure.DataAccess.Models;

public class UserRefreshToken
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public string RefreshToken { get; set; } = null!;

    public DateTime DateCreate { get; set; }
}
