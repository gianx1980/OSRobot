// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Core;

public interface IEvent : IPluginInstance
{
    /// <summary>
    /// Starts the event source. From here on the event reports each occurrence through
    /// <paramref name="sink"/>, until <see cref="IPluginInstance.Destroy"/> is called.
    /// </summary>
    void Init(IEventSink sink);
}
