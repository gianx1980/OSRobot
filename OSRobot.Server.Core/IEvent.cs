// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging.Abstract;

namespace OSRobot.Server.Core;

public class EventTriggeredEventArgs(DynamicDataSet dynamicData, IPluginInstanceLogger logger) : EventArgs
{
    public DynamicDataSet DynamicData { get; set; } = dynamicData;
    public IPluginInstanceLogger Logger { get; set; } = logger;
}

public delegate void EventTriggeredDelegate(object sender, EventTriggeredEventArgs e);

public interface IEvent : IPluginInstance
{
    event EventTriggeredDelegate EventTriggered;
}
