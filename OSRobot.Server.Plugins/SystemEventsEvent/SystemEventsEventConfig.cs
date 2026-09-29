// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

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
