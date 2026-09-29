// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Configuration;
using OSRobot.Server.Models;

namespace OSRobot.Server.Infrastructure.Security.Abstract;

public interface IJWTManager
{
    Tokens CreateToken(UserConfig userConfig);
}
