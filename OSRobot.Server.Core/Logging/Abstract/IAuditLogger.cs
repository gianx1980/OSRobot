// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

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
