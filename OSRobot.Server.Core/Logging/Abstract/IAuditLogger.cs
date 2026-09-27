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
namespace OSRobot.Server.Core.Logging.Abstract;

/// <summary>
/// A distinct logging channel for security-relevant events - logins, lockouts, job-configuration
/// saves, manual task starts - so exercise of OSRobot's (deliberately unsandboxed - see
/// SECURITY.md) automation power is never anonymous. Registered against a separate Serilog sink
/// (ExecLogs/audit-*.log) from the general application log, kept as its own interface (rather
/// than reusing IAppLogger directly) so a class can depend on "the audit log" specifically. Same
/// method shape as IAppLogger by design - AppLogger, a thin wrapper over any Serilog.ILogger,
/// implements both.
/// </summary>
public interface IAuditLogger : IAppLogger
{
}
