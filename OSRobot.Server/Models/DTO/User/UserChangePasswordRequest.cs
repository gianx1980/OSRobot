// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Models.DTO.User;

public class UserChangePasswordRequest(string currentPassword, string newPassword, string confirmPassword)
{
    public string CurrentPassword { get; set; } = currentPassword;
    public string NewPassword { get; set; } = newPassword;
    public string ConfirmPassword { get; set; } = confirmPassword;
}
