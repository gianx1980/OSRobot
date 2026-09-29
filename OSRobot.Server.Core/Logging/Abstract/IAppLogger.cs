// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Core.Logging.Abstract;

public interface IAppLogger
{
    void Info(string message);

    void Info(string message, Exception ex);

    void Error(string message);

    void Error(string message, Exception ex);

    void Warn(string message);

    void Warn(string message, Exception ex);
}
