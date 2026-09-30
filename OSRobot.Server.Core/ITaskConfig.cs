// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json.Serialization;

namespace OSRobot.Server.Core;

public enum IterationMode
{
    IterateDefaultRecordset,
    IterateExactNumber,
    IterateObjectRecordset
}

public interface ITaskConfig : IPluginInstanceConfig
{
    IterationMode PluginIterationMode { get; set; }
    string IterationObject { get; set; }
    int IterationsCount { get; set; }
}
