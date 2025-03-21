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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OSRobot.Server.Configuration;
using OSRobot.Server.Controllers.Base;
using OSRobot.Server.Infrastructure.Security;
using OSRobot.Server.Infrastructure.Security.Abstract;
using OSRobot.Server.Models;
using OSRobot.Server.Models.DTO;
using OSRobot.Server.Models.DTO.User;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace OSRobot.Server.Controllers;


/// <summary>
/// Controller that provide authentication and provision of application's JWT tokens
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AccountController(IJWTManager jWTManager, IOptions<AppSettings> appSettings, IUserRepository userRepository) : AppControllerBase
{
    private readonly IJWTManager _jWTManager = jWTManager;
    private readonly AppSettings _appSettings = appSettings.Value;
    private readonly IUserRepository _userRepository = userRepository;
    
    private string? GetPrincipalNameFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = _appSettings.JWT.Issuer,
            ValidAudience = _appSettings.JWT.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_appSettings.JWT.Key)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        ClaimsPrincipal? principal = null;
        SecurityToken? securityToken = null;
        try
        {
            principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
        }
        catch
        {

        }

        if (principal == null
            || securityToken is not JwtSecurityToken jwtSecurityToken
            || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            return null;

        string? name = principal.Claims.Where(t => t.Type.Contains("nameidentifier")).FirstOrDefault()?.Value;

        return name;
    }


    /// <summary>
    /// Authenticates a user with username and password
    /// </summary>
    /// <param name="userLogin"></param>
    /// <returns>If authentication succesful returns a response with a JWT token</returns>
    [HttpPost]
    [Route("Login")]
    public async Task<IActionResult> Login([FromBody] UserLoginRequest userLogin)
    {
        var loginResult = await _userRepository.Users_Login(userLogin.Username, userLogin.Password);    
        if (loginResult.ResultObject == null
            || loginResult.ResultCode == UserRepositoryResult.WrongCredentials)
        {
            MainResponse<UserLoginResponse> errorResp = new(UserLoginResponse.ResponseWrongCredentials, null, null);
            return Ok(errorResp);
        }

        Tokens token = _jWTManager.CreateToken(new UserConfig() { Id = loginResult.ResultObject.Id, Username = loginResult.ResultObject.UserName });
        if (token.Token == null)
        {
            MainResponse<UserLoginResponse> errorResp = new(UserLoginResponse.ResponseWrongCredentials, "Error during the creation of the token", null);
            return Ok(errorResp);
        }

        // Store the refresh token in the database
        await _userRepository.Users_RefreshTokenSave(loginResult.ResultObject.Id, token.RefreshToken);

        UserLoginResponse userLoginResponse = new(userLogin.Username, token.Token, token.RefreshToken);

        MainResponse<UserLoginResponse> mainResponse = new(MainResponse<UserLoginResponse>.ResponseOk, null, userLoginResponse);
        return Ok(mainResponse);
    }

    [HttpPost]
    [Authorize]
    [Route("ChangePassword")]
    public async Task<IActionResult> ChangePassword([FromBody] UserChangePasswordRequest userChangePassowordRequest)
    {
        if (userChangePassowordRequest.NewPassword != userChangePassowordRequest.ConfirmPassword)
        {
            MainResponse<object?> errorResp = new(MainResponse<object?>.ConfirmPasswordMismatch, "New password / confirm password mismatch", null);
            return Ok(errorResp);
        }

        // Call Users_Login to check current credentials
        var loginResult = await _userRepository.Users_Login(AppUser!.Username, userChangePassowordRequest.CurrentPassword);
        if (loginResult.ResultObject == null
            || loginResult.ResultCode == UserRepositoryResult.WrongCredentials)
        {
            MainResponse<UserLoginResponse> errorResp = new(UserLoginResponse.ResponseWrongCredentials, "Wrong credentials", null);
            return Ok(errorResp);
        }

        await _userRepository.Users_ChangePassword(AppUser!.Id, userChangePassowordRequest.NewPassword);

        MainResponse<object?> mainResponse = new(MainResponse<object?>.ResponseOk, null, null);
        return Ok(mainResponse);
    }

    [HttpPost]
    [Route("RefreshToken")]
    public async Task<IActionResult> RefreshToken([FromBody] UserRefreshTokenRequest userRefreshTokenRequest)
    {        
        string? userName = GetPrincipalNameFromExpiredToken(userRefreshTokenRequest.Token);
        if (userName == null)
            return Unauthorized();

        var repResp = await _userRepository.Users_RefreshTokenValidate(userName, userRefreshTokenRequest.RefreshToken, _appSettings.RefreshToken.ExpireInMinutes);

        // Refresh token doesn't exist or doesn't belong to user or is expired
        if (repResp.ResultCode == UserRepositoryResult.InvalidRefreshToken)
            return Unauthorized();

        Tokens token = _jWTManager.CreateToken(new UserConfig() { Username = userName });
        if (token.Token == null)
        {
            MainResponse<UserRefreshTokenResponse> errorResp = new(MainResponse<UserRefreshTokenResponse>.ResponseGenericError, "Error during the creation of the token", null);
            return Ok(errorResp);
        }

        UserRefreshTokenResponse userRefreshTokenResponse = new(token.Token);
        MainResponse<UserRefreshTokenResponse> mainResponse = new(MainResponse<UserRefreshTokenResponse>.ResponseOk, null, userRefreshTokenResponse);

        return Ok(mainResponse);
    }

    [HttpPost]
    [Authorize]
    [Route("HeartBeat")]
    public IActionResult HeartBeat()
    {
        return Ok();
    }
}

