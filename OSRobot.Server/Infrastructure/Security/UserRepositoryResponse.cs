// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Infrastructure.Security;

public class UserRepositoryResponse<T>(UserRepositoryResult resultCode, T resultObject)
{
    public UserRepositoryResult ResultCode { get; set; } = resultCode;

    public T ResultObject { get; set; } = resultObject;
}
