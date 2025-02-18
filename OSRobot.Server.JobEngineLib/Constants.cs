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
