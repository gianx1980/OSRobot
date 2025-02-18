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

using OSRobot.Server.Core;

namespace OSRobot.Server.Plugins.SystemEventsEvent;

public class SystemEventsEventConfig : IEventConfig
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public bool Log { get; set; } = true;

    public bool EventDisplaySettingsChanged { get; set; }
    public bool EventInstalledFontsChanged { get; set; }
    public bool EventPaletteChanged { get; set; }
    public bool EventPowerModeChanged { get; set; }
    public bool EventSessionEnded { get; set; }
    public bool EventSessionSwitch { get; set; }
    public bool EventTimeChanged { get; set; }
    public bool EventUserPreferenceChanged { get; set; }
}
