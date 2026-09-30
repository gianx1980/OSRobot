// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Core;

[Flags]
public enum EnumOSPlatform
{
    All = 0,
    Windows = 1,
    Linux = 2, 
    MacOS = 4
}
