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
using Microsoft.IdentityModel.Tokens;
using OSRobot.Server.Configuration;
using OSRobot.Server.Infrastructure.Security.Abstract;
using OSRobot.Server.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace OSRobot.Server.Infrastructure.Security;

public class JWTManager : IJWTManager
{
    private readonly IConfiguration _configuration;

    public JWTManager(IConfiguration configuration)
    {
        _configuration = configuration;
    }

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
            Subject = new ClaimsIdentity(new Claim[]
            {
                new(ClaimTypes.Sid, userConfig.Id.ToString()),
                new(ClaimTypes.NameIdentifier, userConfig.Username),
                new(JwtRegisteredClaimNames.Aud, _configuration["AppSettings:JWT:Audience"]!),
                new(JwtRegisteredClaimNames.Iss, _configuration["AppSettings:JWT:Issuer"]!)
            }),
            Expires = DateTime.UtcNow.AddMinutes(double.Parse(_configuration["AppSettings:JWT:ExpireInMinutes"]!)),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(tokenKey), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return new Tokens(tokenHandler.WriteToken(token), GenerateRefreshToken());
    }
}
