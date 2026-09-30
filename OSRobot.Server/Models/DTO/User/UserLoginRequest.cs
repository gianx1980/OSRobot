// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Models.DTO.User;

public class UserLoginRequest(string username, string password)
{
    public string Username { get; set; } = username;
    public string Password { get; set; } = password;
}
