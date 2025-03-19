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
using OSRobot.Server.Core.Logging.Abstract;
using Serilog;

namespace OSRobot.Server.Core.Logging;

public class AppLogger(ILogger logger) : IAppLogger
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
