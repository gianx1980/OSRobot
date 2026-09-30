// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Security.Claims;

namespace OSRobot.Server.Infrastructure.Security;

public class AppTokenUser(ClaimsIdentity identity)
{
    public int Id { get; } = int.Parse(identity.Claims.FirstOrDefault(t => t.Type == ClaimTypes.Sid)!.Value);
    public string Username { get; } = identity.Claims.FirstOrDefault(t => t.Type == ClaimTypes.NameIdentifier)!.Value;
}
