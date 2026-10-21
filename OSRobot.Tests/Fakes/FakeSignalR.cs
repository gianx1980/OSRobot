// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;

namespace OSRobot.Tests.Fakes;

/// <summary>Records every message sent through it.</summary>
public sealed class RecordingClientProxy : IClientProxy
{
    private readonly ConcurrentQueue<(string Method, object?[] Args)> _sent = new();

    public IReadOnlyList<(string Method, object?[] Args)> Sent => [.. _sent];

    public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
    {
        _sent.Enqueue((method, args));
        return Task.CompletedTask;
    }
}

/// <summary>A hub context whose only supported target is Clients.All.</summary>
public sealed class FakeHubContext<THub> : IHubContext<THub> where THub : Hub
{
    private readonly FakeHubClients _clients = new();

    public RecordingClientProxy AllClients => _clients.AllClients;

    public IHubClients Clients => _clients;
    public IGroupManager Groups => throw new NotSupportedException();
}

public sealed class FakeHubClients : IHubClients
{
    public RecordingClientProxy AllClients { get; } = new();

    public IClientProxy All => AllClients;
    public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
    public IClientProxy Client(string connectionId) => throw new NotSupportedException();
    public IClientProxy Clients(IReadOnlyList<string> connectionIds) => throw new NotSupportedException();
    public IClientProxy Group(string groupName) => throw new NotSupportedException();
    public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
    public IClientProxy Groups(IReadOnlyList<string> groupNames) => throw new NotSupportedException();
    public IClientProxy User(string userId) => throw new NotSupportedException();
    public IClientProxy Users(IReadOnlyList<string> userIds) => throw new NotSupportedException();
}

/// <summary>A hub connection for the given user that records whether the hub aborted it.</summary>
public sealed class FakeHubCallerContext(ClaimsPrincipal user) : HubCallerContext
{
    public bool Aborted { get; private set; }

    public override string ConnectionId => "test-connection";
    public override string? UserIdentifier => user.Identity?.Name;
    public override ClaimsPrincipal? User => user;
    public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();
    public override IFeatureCollection Features { get; } = new FeatureCollection();
    public override CancellationToken ConnectionAborted => CancellationToken.None;

    public override void Abort()
    {
        Aborted = true;
    }
}
