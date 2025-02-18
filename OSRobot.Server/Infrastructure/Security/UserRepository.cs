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
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OSRobot.Server.Infrastructure.Security.Abstract;
using OSRobot.Server.Infrastructure.DataAccess.Models;
using OSRobot.Server.Core;
using Irony.Parsing;

namespace OSRobot.Server.Infrastructure.Security;

public class UserRepository : IUserRepository, IDisposable
{
    private const int _saltLength = 64;
    private readonly char[] _charList = @"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789;:.,-_@!£$%&/()=?^'\|[]{}".ToCharArray();
    private readonly RobotDBContext _dbContext;

    private async Task<string> _getPasswordHash(string password, string salt)
    {
        using SHA512 sha512 = SHA512.Create();
        return Convert.ToBase64String(await sha512.ComputeHashAsync(new MemoryStream(Encoding.UTF8.GetBytes(salt + password))));
    }

    private string _getSalt()
    {
        return RandomNumberGenerator.GetString(_charList, _saltLength);
    }

    public UserRepository(RobotDBContext context)
    {
        _dbContext = context;
    }

    public async Task<UserRepositoryResponse<User?>> Users_Login(string userName, string password)
    {
        User? user = await _dbContext.Users.Where(u => u.UserName == userName).FirstOrDefaultAsync();

        // User not found
        if (user == null)
            return new UserRepositoryResponse<User?>(UserRepositoryResult.WrongCredentials, null);

        // Check password
        string passwordHash = await _getPasswordHash(password, user.Salt);
        if (user.Password != passwordHash)
            return new UserRepositoryResponse<User?>(UserRepositoryResult.WrongCredentials, null);

        return new UserRepositoryResponse<User?>(UserRepositoryResult.Ok, user);
    }

    public Task<UserRepositoryResponse<object?>> Users_Save(User user)
    {
        throw new NotImplementedException();
    }

    public async Task<UserRepositoryResponse<object?>> Users_ChangePassword(long userId, string newPassword)
    {
        User? user = await _dbContext.Users.Where(u => u.Id == userId).FirstOrDefaultAsync();
        if (user == null)
            return new UserRepositoryResponse<object?>(UserRepositoryResult.InvalidUser, null);

        string salt = _getSalt();
        string passwordHash = await _getPasswordHash(newPassword, salt);

        user.Salt = salt;
        user.Password = passwordHash;
        await _dbContext.SaveChangesAsync();

        return new UserRepositoryResponse<object?>(UserRepositoryResult.Ok, null);
    }

    public async Task<UserRepositoryResponse<object?>> Users_RefreshTokenSave(long userId, string refreshToken)
    {
        UserRefreshToken userRefreshToken = new UserRefreshToken()
        {
            UserId = userId,
            RefreshToken = refreshToken,
            DateCreate = DateTime.Now
        };

        _dbContext.UserRefreshTokens.Add(userRefreshToken);
        await _dbContext.SaveChangesAsync();

        return new UserRepositoryResponse<object?>(UserRepositoryResult.Ok, null);
    }

    public async Task<UserRepositoryResponse<object?>> Users_RefreshTokenValidate(string userName, string refreshToken, int tokenDurationMinutes)
    {
        var result = await _dbContext.UserRefreshTokens.Join(_dbContext.Users,
                                                                urt => urt.UserId,
                                                                u => u.Id,
                                                                (urt, u) => new {   u.UserName, 
                                                                                    urt.RefreshToken, 
                                                                                    urt.DateCreate
                                                                                })
                                                            .Where(t => t.UserName == userName && t.RefreshToken == refreshToken)
                                                            .FirstOrDefaultAsync();

        // Refresh token not found or not belong to user
        if (result == null)
            return new UserRepositoryResponse<object?>(UserRepositoryResult.InvalidRefreshToken, null);

        // Refresh token expired
        if (result.DateCreate.AddMinutes(tokenDurationMinutes) < DateTime.Now)
            return new UserRepositoryResponse<object?>(UserRepositoryResult.InvalidRefreshToken, null);

        return new UserRepositoryResponse<object?>(UserRepositoryResult.RefreshTokenOk, null);
    }

    // IDisposable interface
    private bool _disposed = false;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _dbContext.Dispose();
            }
        }
        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
