// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Runtime.Versioning;
using System.ServiceProcess;

namespace OSRobot.Server.Plugins.ServiceProcessWatchEvent;

/// <summary>
/// The values of ServiceControllerStatus, which is marked Windows-only: with its own copy, the change detection
/// builds and runs on any platform.
/// </summary>
public enum ServiceStatus
{
    Stopped = 1,
    StartPending = 2,
    StopPending = 3,
    Running = 4,
    ContinuePending = 5,
    PausePending = 6,
    Paused = 7
}

public readonly record struct ServiceInfo(string ServiceName, string DisplayName, ServiceStatus Status);

public sealed record ServiceStateChange(string ServiceName, string DisplayName, ServiceStatus PreviousState, ServiceStatus State) : WatchChange;

/// <summary>
/// Reports the services matching the name pattern whose state changed since the previous check. A check only
/// sees the state at that moment: a service that stops and starts again between two checks is not reported,
/// and pending states in between may be skipped.
/// </summary>
public sealed class ServiceWatcher(ServiceProcessWatchEventConfig config, Func<IReadOnlyList<ServiceInfo>> readServices) : IWatcher
{
    private readonly ServiceProcessWatchEventConfig _config = config;
    private readonly Func<IReadOnlyList<ServiceInfo>> _readServices = readServices;
    private readonly NamePattern _pattern = new(config.NamePattern);

    // Null until the baseline is taken. Service names are case-insensitive.
    private Dictionary<string, ServiceStatus>? _previous;

    public IReadOnlyList<WatchChange> Check()
    {
        Dictionary<string, ServiceStatus> current = new(StringComparer.OrdinalIgnoreCase);
        List<WatchChange> changes = [];

        foreach (ServiceInfo service in _readServices())
        {
            if (!_pattern.IsMatch(service.ServiceName) && !_pattern.IsMatch(service.DisplayName))
                continue;

            current[service.ServiceName] = service.Status;

            // A service installed since the previous check has no state to change from: it joins silently.
            if (_previous != null
                && _previous.TryGetValue(service.ServiceName, out ServiceStatus previous)
                && previous != service.Status
                && IsWatched(service.Status))
                changes.Add(new ServiceStateChange(service.ServiceName, service.DisplayName, previous, service.Status));
        }

        _previous = current;
        return changes;
    }

    private bool IsWatched(ServiceStatus state) =>
        _config.OnAnyServiceStateChange
        || (state == ServiceStatus.Stopped && _config.OnServiceStopped)
        || (state == ServiceStatus.Running && _config.OnServiceStarted)
        || (state == ServiceStatus.Paused && _config.OnServicePaused);

    public void Dispose() { }
}

[SupportedOSPlatform("windows")]
public static class WindowsServices
{
    /// <summary>The services (drivers left out), states included, read by a single enumeration of the service manager.</summary>
    public static IReadOnlyList<ServiceInfo> Read()
    {
        ServiceController[] services = ServiceController.GetServices();
        List<ServiceInfo> result = new(services.Length);

        foreach (ServiceController service in services)
        {
            using (service)
                result.Add(new ServiceInfo(service.ServiceName, service.DisplayName, (ServiceStatus)(int)service.Status));
        }

        return result;
    }
}
