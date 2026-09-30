// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Models;

public class Tokens(string token, string refreshToken)
{
    public string Token { get; set; } = token;
    public string RefreshToken { get; set; } = refreshToken;
}
