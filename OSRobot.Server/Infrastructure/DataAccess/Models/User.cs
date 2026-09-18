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
