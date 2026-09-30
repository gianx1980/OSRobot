// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Infrastructure.DataAccess.Models;

public partial class User
{
    public long Id { get; set; }

    public string UserName { get; set; } = null!;

    public string Salt { get; set; } = null!;

    public string Password { get; set; } = null!;

    /// <summary>True until the user changes their password at least once. The seeded default
    /// admin account starts true so the default credentials can't be left in place unnoticed.</summary>
    public bool MustChangePassword { get; set; }

    /// <summary>Consecutive failed login attempts since the last success. Reset on success.</summary>
    public int FailedLoginAttempts { get; set; }

    /// <summary>If set and in the future, login is refused regardless of password correctness.</summary>
    public DateTime? LockedOutUntil { get; set; }
}
