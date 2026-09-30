// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.JobEngineLib;

static class Constants
{
    internal const string DefaultLogPath = @"Log\";
    internal const string DefaultLibPath = @"Lib\";
    internal const string DefaultDataPath = @"Data\";
    internal const bool DefaultSerialExecution = false;
    internal const int CleanUpLogsOlderThanHours = 0;
    internal const int CleanUpLogsIntervalHours = 0;
    internal const int HttpListenerPort = 44300;
}
