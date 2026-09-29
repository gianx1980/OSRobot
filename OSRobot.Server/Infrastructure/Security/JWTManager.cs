// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.IdentityModel.Tokens;
using OSRobot.Server.Configuration;
using OSRobot.Server.Infrastructure.Security.Abstract;
using OSRobot.Server.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace OSRobot.Server.Infrastructure.Security;

public class JWTManager(IConfiguration configuration) : IJWTManager
{
    private readonly IConfiguration _configuration = configuration;

    private string GenerateRefreshToken()
    {
        byte[] randomNumber = new byte[32];
        using RandomNumberGenerator rng = RandomNumberGenerator.Create();
        
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public Tokens CreateToken(UserConfig userConfig)
    {
        // Check token configuration parameters
        if (string.IsNullOrEmpty(_configuration["AppSettings:JWT:Key"])
            || string.IsNullOrEmpty(_configuration["AppSettings:JWT:Audience"])
            || string.IsNullOrEmpty(_configuration["AppSettings:JWT:Issuer"])
            || string.IsNullOrEmpty(_configuration["AppSettings:JWT:ExpireInMinutes"])
            || !double.TryParse(_configuration["AppSettings:JWT:ExpireInMinutes"], out _)
            )
            throw new ApplicationException("Invalid or missing token parameters in configuration");
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenKey = Encoding.UTF8.GetBytes(_configuration["AppSettings:JWT:Key"]!);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new(ClaimTypes.Sid, userConfig.Id.ToString()),
                new(ClaimTypes.NameIdentifier, userConfig.Username),
                new(OSRobotClaimTypes.MustChangePassword, userConfig.MustChangePassword ? "true" : "false"),
                new(JwtRegisteredClaimNames.Aud, _configuration["AppSettings:JWT:Audience"]!),
                new(JwtRegisteredClaimNames.Iss, _configuration["AppSettings:JWT:Issuer"]!)
            ]),
            Expires = DateTime.UtcNow.AddMinutes(double.Parse(_configuration["AppSettings:JWT:ExpireInMinutes"]!)),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(tokenKey), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return new Tokens(tokenHandler.WriteToken(token), GenerateRefreshToken());
    }
}
