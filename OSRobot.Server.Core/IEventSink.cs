// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging.Abstract;

namespace OSRobot.Server.Core;

/// <summary>
/// Where an event reports that it occurred. Implemented by the job engine, which queues the
/// connected tasks: Publish never runs them inline, so it is safe (and cheap) to call from a
/// timer or FileSystemWatcher callback thread.
/// </summary>
public interface IEventSink
{
    /// <returns>false if the occurrence was ignored because the engine is not accepting work (stopping/reloading).</returns>
    bool Publish(IEvent source, DynamicDataSet dynamicData, IPluginInstanceLogger logger);
}
