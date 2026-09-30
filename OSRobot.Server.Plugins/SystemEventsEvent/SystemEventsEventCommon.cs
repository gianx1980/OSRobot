// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Plugins.SystemEventsEvent;

internal class SystemEventsEventCommon
{
    public const string DynDataKeyEventCode = "EventCode";

    public const string EventCodeUserPreferenceChanged = "UserPreferenceChanged";
    public const string EventCodeTimeChanged = "TimeChanged";
    public const string EventCodeSessionSwitch = "SessionSwitch";
    public const string EventCodeSessionEnded = "SessionEnded";
    public const string EventCodePowerModeChanged = "PowerModeChanged";
    public const string EventCodePaletteChanged = "PaletteChanged";
    public const string EventCodeInstalledFontsChanged = "InstalledFontsChanged";
    public const string EventCodeDisplaySettingsChanged = "DisplaySettingsChanged";
}
