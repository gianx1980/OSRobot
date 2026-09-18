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
using Microsoft.Extensions.Options;
using OSRobot.Server.Configuration;
using OSRobot.Server.Infrastructure.Security.Abstract;
using OSRobot.Server.Infrastructure.DataAccess.Models;

namespace OSRobot.Server.Infrastructure.Security;

public class UserRepository(RobotDBContext context, IOptions<AppSettings> appSettings) : IUserRepository, IDisposable
{
    private const int _saltSizeBytes = 16;                 // 128-bit salt
    private const int _pbkdf2KeyLengthBytes = 32;           // 256-bit derived key
    private const int _pbkdf2IterationCount = 210_000;      // OWASP 2023 minimum recommendation for PBKDF2-HMAC-SHA256
    private const string _pbkdf2Prefix = "PBKDF2$SHA256$";  // stored Password format: PBKDF2$SHA256$<iterations>$<base64 hash>

    // Fixed, precomputed salt/hash used only to keep Users_Login's timing profile the same
    // whether or not the requested username exists - see the comment in Users_Login.
    private static readonly string _dummySalt = GetSalt();
    private static readonly string _dummyPasswordField = HashPassword("no-such-user-dummy-password", _dummySalt, _pbkdf2IterationCount);

    private readonly RobotDBContext _dbContext = context;
    private readonly SecurityConfig _securityConfig = appSettings.Value.Security;

    private static string GetSalt()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(_saltSizeBytes));
    }

    private static string HashPassword(string password, string saltBase64, int iterations)
    {
        byte[] salt = Convert.FromBase64String(saltBase64);
        byte[] key = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, _pbkdf2KeyLengthBytes);
        return $"{_pbkdf2Prefix}{iterations}${Convert.ToBase64String(key)}";
    }

    private static bool VerifyPbkdf2(string password, string saltBase64, string storedPasswordField)
    {
        // Format: PBKDF2$SHA256$<iterations>$<base64 hash>
        string[] parts = storedPasswordField.Split('$');
        if (parts.Length != 4 || !int.TryParse(parts[2], out int iterations))
            return false;

        byte[] salt = Convert.FromBase64String(saltBase64);
        byte[] expected = Convert.FromBase64String(parts[3]);
        byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);

        // Constant-time comparison: a plain byte[]/string equality check returns as soon as it
        // finds the first differing byte, so its running time leaks how many leading bytes of
        // the guess were correct. FixedTimeEquals always compares the full length instead.
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    // Pre-PBKDF2 scheme: single-pass salted SHA-512, Base64. Kept only so an existing database
    // seeded/changed before this change still authenticates; VerifyPassword transparently
    // upgrades a match to PBKDF2 (see RehashPasswordAsync).
    private static bool VerifyLegacySha512(string password, string saltBase64, string storedPasswordField)
    {
        using SHA512 sha512 = SHA512.Create();
        byte[] actual = sha512.ComputeHash(Encoding.UTF8.GetBytes(saltBase64 + password));

        byte[] expected;
        try
        {
            expected = Convert.FromBase64String(storedPasswordField);
        }
        catch (FormatException)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    /// <returns>true if <paramref name="password"/> matches; <paramref name="needsRehash"/> is
    /// true when the match was against the legacy SHA-512 scheme and should be upgraded.</returns>
    private static bool VerifyPassword(string password, string salt, string storedPasswordField, out bool needsRehash)
    {
        try
        {
            if (storedPasswordField.StartsWith(_pbkdf2Prefix, StringComparison.Ordinal))
            {
                needsRehash = false;
                return VerifyPbkdf2(password, salt, storedPasswordField);
            }

            bool matched = VerifyLegacySha512(password, salt, storedPasswordField);
            needsRehash = matched;
            return matched;
        }
        catch (FormatException)
        {
            // Malformed/corrupt stored salt or hash - treat as a non-match rather than a 500.
            needsRehash = false;
            return false;
        }
    }

    private async Task RehashPasswordAsync(User user, string plainTextPassword)
    {
        string newSalt = GetSalt();
        user.Salt = newSalt;
        user.Password = HashPassword(plainTextPassword, newSalt, _pbkdf2IterationCount);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<UserRepositoryResponse<User?>> Users_Login(string userName, string password)
    {
        User? user = await _dbContext.Users.Where(u => u.UserName == userName).FirstOrDefaultAsync();

        // Locked out: refuse before spending a PBKDF2 derivation on a password that wouldn't be
        // checked anyway. This is a deliberately disclosed state (unlike "wrong password" vs
        // "no such user"), so short-circuiting here doesn't reopen the timing side-channel below.
        if (user != null && user.LockedOutUntil is DateTime lockedOutUntil && lockedOutUntil > DateTime.Now)
            return new UserRepositoryResponse<User?>(UserRepositoryResult.AccountLockedOut, null);

        // Always run a full password verification - using a fixed dummy salt/hash when the
        // username doesn't exist - so a request for a nonexistent username takes essentially
        // the same time as one for a real username with a wrong password. Returning immediately
        // on "user not found" would let an attacker enumerate valid usernames purely from
        // response timing (a missing user skips the ~expensive PBKDF2 derivation entirely).
        bool passwordOk = VerifyPassword(password, user?.Salt ?? _dummySalt, user?.Password ?? _dummyPasswordField, out bool needsRehash);

        if (user == null)
            return new UserRepositoryResponse<User?>(UserRepositoryResult.WrongCredentials, null);

        if (!passwordOk)
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= _securityConfig.MaxFailedLoginAttempts)
                user.LockedOutUntil = DateTime.Now.AddMinutes(_securityConfig.LockoutDurationMinutes);

            await _dbContext.SaveChangesAsync();
            return new UserRepositoryResponse<User?>(UserRepositoryResult.WrongCredentials, null);
        }

        bool lockoutStateChanged = user.FailedLoginAttempts != 0 || user.LockedOutUntil != null;
        user.FailedLoginAttempts = 0;
        user.LockedOutUntil = null;

        if (needsRehash)
            await RehashPasswordAsync(user, password);     // saves the whole entity, including the reset above
        else if (lockoutStateChanged)
            await _dbContext.SaveChangesAsync();

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

        user.MustChangePassword = false;
        await RehashPasswordAsync(user, newPassword);

        return new UserRepositoryResponse<object?>(UserRepositoryResult.Ok, null);
    }

    public async Task<UserRepositoryResponse<object?>> Users_RefreshTokenSave(long userId, string refreshToken)
    {
        UserRefreshToken userRefreshToken = new()
        {
            UserId = userId,
            RefreshToken = refreshToken,
            DateCreate = DateTime.Now
        };

        _dbContext.UserRefreshTokens.Add(userRefreshToken);
        await _dbContext.SaveChangesAsync();

        return new UserRepositoryResponse<object?>(UserRepositoryResult.Ok, null);
    }

    public async Task<UserRepositoryResponse<User?>> Users_RefreshTokenValidate(string userName, string refreshToken, int tokenDurationMinutes)
    {
        var result = await _dbContext.UserRefreshTokens.Join(_dbContext.Users,
                                                                urt => urt.UserId,
                                                                u => u.Id,
                                                                (urt, u) => new {   User = u,
                                                                                    urt.RefreshToken,
                                                                                    urt.DateCreate
                                                                                })
                                                            .Where(t => t.User.UserName == userName && t.RefreshToken == refreshToken)
                                                            .FirstOrDefaultAsync();

        // Refresh token not found or not belong to user
        if (result == null)
            return new UserRepositoryResponse<User?>(UserRepositoryResult.InvalidRefreshToken, null);

        // Refresh token expired
        if (result.DateCreate.AddMinutes(tokenDurationMinutes) < DateTime.Now)
            return new UserRepositoryResponse<User?>(UserRepositoryResult.InvalidRefreshToken, null);

        // Returned so the caller can mint a fresh token with an up-to-date MustChangePassword
        // claim, rather than copying a possibly-stale one from the expired token being refreshed.
        return new UserRepositoryResponse<User?>(UserRepositoryResult.RefreshTokenOk, result.User);
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
