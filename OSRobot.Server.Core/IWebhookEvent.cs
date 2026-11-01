// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.RegularExpressions;

namespace OSRobot.Server.Core;

/// <summary>An HTTP call to /hooks/{path}, free of ASP.NET types: plugins don't depend on the web host.</summary>
public sealed record WebhookRequest(string ContentType, byte[] Body, IReadOnlyList<KeyValuePair<string, string>> Headers,
                                    IReadOnlyList<KeyValuePair<string, string>> Query, string SourceIp);

public enum WebhookDeliveryResult
{
    /// <summary>The run was queued.</summary>
    Accepted,
    /// <summary>No enabled hook has this path.</summary>
    NotFound,
    /// <summary>Wrong token or signature. Answered like NotFound, so nobody can probe which hooks exist.</summary>
    Unauthorized,
    /// <summary>The hook's calls-per-minute limit was reached.</summary>
    TooManyRequests,
    /// <summary>The body doesn't fit the hook's settings (not JSON although a JSON path is set).</summary>
    BadRequest,
    /// <summary>The engine is stopping or reloading: the caller can retry.</summary>
    EngineNotAccepting
}

/// <summary>
/// An event started by an HTTP call (the Webhook event). The engine finds it by <see cref="Path"/>, and calls
/// <see cref="Deliver"/> on the request's thread: it must only check the call and publish, never run anything.
/// </summary>
public interface IWebhookEvent : IEvent
{
    /// <summary>The last segment of the URL. Unique among the enabled hooks.</summary>
    string Path { get; }

    /// <summary>What authenticates the calls, as configured (normally a {secret['Name']} placeholder).</summary>
    string Token { get; }

    WebhookDeliveryResult Deliver(WebhookRequest request);
}

public static partial class WebhookPath
{
    public const int MaxLength = 100;

    public static bool IsValid(string path) => path.Length <= MaxLength && PathRegex().IsMatch(path);

    [GeneratedRegex("^[A-Za-z0-9_-]+$")]
    private static partial Regex PathRegex();
}
