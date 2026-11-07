// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Plugins.ServiceProcessWatchEvent;

/// <summary>A change seen between two checks.</summary>
public abstract record WatchChange;

/// <summary>
/// Compares what it reads at each check with what it read at the previous one. The first check is the baseline:
/// it fires nothing, so the event doesn't report everything already stopped or running when it starts. A first
/// check that fails leaves the baseline to the next one.
/// </summary>
public interface IWatcher : IDisposable
{
    /// <summary>The changes since the previous check that the configuration asks for.</summary>
    IReadOnlyList<WatchChange> Check();
}
