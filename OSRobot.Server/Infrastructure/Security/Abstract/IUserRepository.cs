// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Infrastructure.DataAccess.Models;

namespace OSRobot.Server.Infrastructure.Security.Abstract;

public interface IUserRepository
{
    public Task<UserRepositoryResponse<User?>> Users_Login(string userName, string password);

    public Task<UserRepositoryResponse<object?>> Users_ChangePassword(long userId, string newPassword);

    public Task<UserRepositoryResponse<object?>> Users_RefreshTokenSave(long userId, string refreshToken);

    public Task<UserRepositoryResponse<User?>> Users_RefreshTokenValidate(string userName, string refreshToken, int tokenDurationMinutes);
}
