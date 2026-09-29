// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Infrastructure.Security;

/// <summary>Custom JWT claim types, shared between the code that mints tokens (JWTManager) and
/// the code that reads them back (MustChangePasswordFilter).</summary>
public static class OSRobotClaimTypes
{
    public const string MustChangePassword = "mustChangePassword";
}
