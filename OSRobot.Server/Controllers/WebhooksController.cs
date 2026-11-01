// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using OSRobot.Server.Configuration;
using OSRobot.Server.Core;
using OSRobot.Server.Core.Logging.Abstract;
using OSRobot.Server.Infrastructure.Webhooks;
using OSRobot.Server.JobEngineLib.Infrastructure.Abstract;

namespace OSRobot.Server.Controllers;

/// <summary>
/// Calls from other systems, starting a Webhook event. The only anonymous way to start a job: the hook itself
/// checks the token or signature. Outside /api so a proxy can publish this path alone. See SECURITY.md.
/// </summary>
[ApiController]
[Route("hooks")]
public class WebhooksController(IJobEngine jobEngine, IOptions<AppSettings> appSettings, IAuditLogger auditLogger) : ControllerBase
{
    private readonly IJobEngine _jobEngine = jobEngine;
    private readonly WebhooksConfig _config = appSettings.Value.Webhooks;
    private readonly IAuditLogger _auditLogger = auditLogger;

    [HttpPost("{path}")]
    [AllowAnonymous]
    [EnableRateLimiting(WebhookEndpoint.RateLimiterPolicy)]
    public async Task<IActionResult> Call()
    {
        // From the route, not an action parameter: binding one makes MVC read a form-encoded body, consuming it
        // before it can be read here, and a signature would then be checked against nothing.
        string path = (string)RouteData.Values["path"]!;

        byte[]? body = await ReadBodyAsync();
        if (body == null)
            return StatusCode(StatusCodes.Status413PayloadTooLarge);

        string sourceIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        WebhookRequest request = new(Request.ContentType ?? string.Empty, body,
                                     [.. Request.Headers.Select(h => KeyValuePair.Create(h.Key, h.Value.ToString()))],
                                     [.. Request.Query.Select(q => KeyValuePair.Create(q.Key, q.Value.ToString()))],
                                     sourceIp);

        WebhookDeliveryResult result = _jobEngine.DeliverWebhook(path, request);

        switch (result)
        {
            case WebhookDeliveryResult.Accepted:
                return Accepted();

            case WebhookDeliveryResult.Unauthorized:
                // The path is an existing hook's: safe to write to the log, unlike whatever an unknown one contains.
                _auditLogger.Warn($"Webhook '{path}': call with a wrong token or signature from {sourceIp}.");
                return NotFound();

            case WebhookDeliveryResult.TooManyRequests:
                return StatusCode(StatusCodes.Status429TooManyRequests);

            case WebhookDeliveryResult.BadRequest:
                return BadRequest();

            case WebhookDeliveryResult.EngineNotAccepting:
                return StatusCode(StatusCodes.Status503ServiceUnavailable);

            default:
                return NotFound();
        }
    }

    // The body up to the limit, or null if it is larger. Read in full before the hook sees it: a signature covers all of it.
    private async Task<byte[]?> ReadBodyAsync()
    {
        IHttpMaxRequestBodySizeFeature? sizeFeature = HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (sizeFeature is { IsReadOnly: false })
            sizeFeature.MaxRequestBodySize = _config.MaxBodyBytes;

        if (Request.ContentLength > _config.MaxBodyBytes)
            return null;

        using MemoryStream buffer = new();
        byte[] chunk = new byte[16 * 1024];
        try
        {
            int read;
            while ((read = await Request.Body.ReadAsync(chunk, HttpContext.RequestAborted)) > 0)
            {
                if (buffer.Length + read > _config.MaxBodyBytes)
                    return null;

                buffer.Write(chunk, 0, read);
            }
        }
        catch (BadHttpRequestException ex) when (ex.StatusCode == StatusCodes.Status413PayloadTooLarge)
        {
            return null;
        }

        return buffer.ToArray();
    }
}
