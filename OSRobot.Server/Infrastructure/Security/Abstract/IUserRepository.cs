/*======================================================================================
    Copyright 2025 by Gianluca Di Bucci (gianx1980) (https://www.os-robot.com)

    This file is part of OSRobot.

    OSRobot is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    OSRobot is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with OSRobot.  If not, see <http://www.gnu.org/licenses/>.
======================================================================================*/
using OSRobot.Server.Infrastructure.DataAccess.Models;

namespace OSRobot.Server.Infrastructure.Security.Abstract;

public interface IUserRepository
{
    public Task<UserRepositoryResponse<User?>> Users_Login(string userName, string password);

    public Task<UserRepositoryResponse<object?>> Users_Save(User user);

    public Task<UserRepositoryResponse<object?>> Users_ChangePassword(long userId, string newPassword);

    public Task<UserRepositoryResponse<object?>> Users_RefreshTokenSave(long userId, string refreshToken);

    public Task<UserRepositoryResponse<object?>> Users_RefreshTokenValidate(string userName, string refreshToken, int tokenDurationMinutes);
}
