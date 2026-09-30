// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core.Logging.Abstract;
using Serilog;

namespace OSRobot.Server.Core.Logging;

// Also satisfies IAuditLogger: both interfaces share the same Info/Error/Warn shape, and a
// second instance of this class, wrapping a separate Serilog sink, is registered as IAuditLogger
// in Program.cs.
public class AppLogger(ILogger logger) : IAppLogger, IAuditLogger
{
    private readonly ILogger _logger = logger;

    public void Error(string message)
    {
        _logger.Error(message);
    }

    public void Error(string message, Exception ex)
    {
        _logger.Error(ex, message);
    }

    public void Info(string message)
    {
        _logger.Information(message);
    }

    public void Info(string message, Exception ex)
    {
        _logger.Information(ex, message);
    }

    public void Warn(string message)
    {
        _logger.Warning(message);
    }

    public void Warn(string message, Exception ex)
    {
        _logger.Warning(ex, message);
    }
}
