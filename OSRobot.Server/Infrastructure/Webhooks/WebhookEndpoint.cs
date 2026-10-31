// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using OSRobot.Server.Configuration;

namespace OSRobot.Server.Infrastructure.Webhooks;

/// <summary>
/// How the hooks are reached from other machines. The server listens on addresses and ports, not on URLs, so
/// a single URL can't be opened on its own: instead, the Kestrel endpoint named "Webhooks" serves POST /hooks/
/// and nothing else, while the main endpoint (editor, login, API) stays on loopback. The restriction follows
/// from the endpoint's name, not from a separate setting that could be forgotten and expose the whole API.
/// Without that endpoint nothing changes: the hooks answer on the main endpoint only.
/// </summary>
public static class WebhookEndpoint
{
    public const string EndpointName = "Webhooks";
    public const string RoutePrefix = "/hooks";
    public const string RateLimiterPolicy = "webhooks";

    /// <summary>The port of the "Webhooks" endpoint, or null if there is none.</summary>
    /// <exception cref="InvalidOperationException">Its URL has no usable port: the server must not start with an unrestricted endpoint.</exception>
    public static int? GetWebhooksOnlyPort(IConfiguration configuration)
    {
        string? url = configuration[$"Kestrel:Endpoints:{EndpointName}:Url"];
        if (string.IsNullOrWhiteSpace(url))
            return null;

        // Kestrel's wildcards are not valid in a Uri.
        string parsable = url.Replace("://*", "://localhost").Replace("://+", "://localhost");
        if (!Uri.TryCreate(parsable, UriKind.Absolute, out Uri? uri) || uri.Port <= 0)
            throw new InvalidOperationException($"Kestrel:Endpoints:{EndpointName}:Url must include a port: '{url}'.");

        return uri.Port;
    }

    public static IServiceCollection AddOSRobotWebhooks(this IServiceCollection services, WebhooksConfig config)
    {
        // By address: a flood of bad calls doesn't use up a hook's own limit, which only counts authenticated ones.
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(RateLimiterPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                _ => new FixedWindowRateLimiterOptions { PermitLimit = Math.Max(1, config.MaxRequestsPerMinutePerIp), Window = TimeSpan.FromMinutes(1) }));
        });

        return services;
    }

    /// <summary>Whether a request arriving on the webhooks-only endpoint may go on.</summary>
    public static bool IsAllowedOnWebhooksOnlyEndpoint(string method, PathString path)
    {
        return HttpMethods.IsPost(method) && path.StartsWithSegments(RoutePrefix);
    }

    /// <summary>
    /// Deny by default on that endpoint, ahead of everything else, so anything added to the server later stays
    /// private without having to remember this. 404, like an unknown hook: the port reveals nothing else.
    /// </summary>
    public static IApplicationBuilder UseWebhooksOnlyEndpoint(this IApplicationBuilder app, int? webhooksOnlyPort)
    {
        if (webhooksOnlyPort == null)
            return app;

        return app.Use(async (context, next) =>
        {
            if (context.Connection.LocalPort == webhooksOnlyPort && !IsAllowedOnWebhooksOnlyEndpoint(context.Request.Method, context.Request.Path))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            await next();
        });
    }
}
