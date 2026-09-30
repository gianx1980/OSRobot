// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.JobEngineLib.Infrastructure.Abstract;

namespace OSRobot.Server.Infrastructure.Hosting;

/// <summary>
/// Adapts <see cref="IJobEngine"/> to the generic host lifecycle.
///
/// This makes the engine start only after the host has finished building all
/// services (rather than inline in Program.cs, before the host even exists), and
/// stop as part of an orderly host shutdown - along with every other hosted
/// service, honoring <c>HostOptions.ShutdownTimeout</c> via the cancellation token
/// passed to <see cref="StopAsync"/>. DI owns the engine's lifetime.
/// </summary>
public class JobEngineHostedService(IJobEngine jobEngine, ILogger<JobEngineHostedService> logger) : IHostedService
{
    private readonly IJobEngine _jobEngine = jobEngine;
    private readonly ILogger<JobEngineHostedService> _logger = logger;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting JobEngine...");
        _jobEngine.Start(cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping JobEngine...");
        _jobEngine.Stop(cancellationToken);
        return Task.CompletedTask;
    }
}
