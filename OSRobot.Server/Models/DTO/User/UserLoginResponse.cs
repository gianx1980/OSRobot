// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Models.DTO.User;

public class UserLoginResponse(string username, string token, string refreshToken, bool mustChangePassword)
{
    public string Username { get; set; } = username;

    public string Token { get; set; } = token;

    public string RefreshToken { get; set; } = refreshToken;

    public bool MustChangePassword { get; set; } = mustChangePassword;
}
