// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;

namespace OSRobot.Server.JobEngineLib;

static class Common
{
    //Root folder: contains all events, tasks, folders...
    internal static IFolder? RootFolder { get; set; }
}
