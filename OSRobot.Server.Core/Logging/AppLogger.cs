// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core.Logging.Abstract;
using Serilog;

namespace OSRobot.Server.Core.Logging;

// Also satisfies IAuditLogger: both interfaces share the same Info/Error/Warn shape, and a
// second instance of this class, wrapping a separate Serilog sink, is registered as IAuditLogger
// in Program.cs.
// Messages are redacted like the execution logs (a message may quote a value that holds a secret);
// exceptions are written as Serilog formats them.
public class AppLogger(ILogger logger) : IAppLogger, IAuditLogger
{
    private readonly ILogger _logger = logger;

    public void Error(string message)
    {
        _logger.Error(Core.Variables.Redact(message));
    }

    public void Error(string message, Exception ex)
    {
        _logger.Error(ex, Core.Variables.Redact(message));
    }

    public void Info(string message)
    {
        _logger.Information(Core.Variables.Redact(message));
    }

    public void Info(string message, Exception ex)
    {
        _logger.Information(ex, Core.Variables.Redact(message));
    }

    public void Warn(string message)
    {
        _logger.Warning(Core.Variables.Redact(message));
    }

    public void Warn(string message, Exception ex)
    {
        _logger.Warning(ex, Core.Variables.Redact(message));
    }
}
