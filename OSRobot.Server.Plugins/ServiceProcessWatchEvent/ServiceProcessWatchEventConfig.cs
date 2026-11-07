// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json.Serialization;
using OSRobot.Server.Core;

namespace OSRobot.Server.Plugins.ServiceProcessWatchEvent;

public enum WatchType
{
    Service,
    Process
}

public class ServiceProcessWatchEventConfig : IEventConfig
{
    private const int _defaultCheckIntervalSeconds = 5;

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public bool Log { get; set; } = true;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public WatchType WatchType { get; set; } = WatchType.Service;

    /// <summary>Names to watch, separated by ";", with * and ? wildcards: see <see cref="NamePattern"/>.</summary>
    public string NamePattern { get; set; } = string.Empty;

    public bool OnServiceStopped { get; set; } = true;

    /// <summary>The service enters Running: started, or resumed after a pause.</summary>
    public bool OnServiceStarted { get; set; }

    public bool OnServicePaused { get; set; }

    /// <summary>Every change of state, the pending ones (StartPending, StopPending...) included.</summary>
    public bool OnAnyServiceStateChange { get; set; }

    public bool OnProcessStarted { get; set; }

    public bool OnProcessExited { get; set; } = true;

    public int CheckIntervalSeconds { get; set; } = _defaultCheckIntervalSeconds;
}
