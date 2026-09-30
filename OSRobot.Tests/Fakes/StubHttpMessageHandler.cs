// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Net;
using System.Text;

namespace OSRobot.Tests.Fakes;

/// <summary>A request as seen by the stub, with the body already read (the content is disposed after sending).</summary>
public sealed record RecordedRequest(HttpMethod Method, Uri Uri, string? Body, string? ContentType, IReadOnlyDictionary<string, string> Headers);

/// <summary>
/// An HttpMessageHandler that answers from a delegate instead of the network, and records every request.
/// No socket is opened, so "https://" URLs need no certificate, DNS or connectivity.
/// </summary>
public sealed class StubHttpMessageHandler(Func<RecordedRequest, HttpResponseMessage> responder) : HttpMessageHandler
{
    private readonly object _gate = new();
    private readonly List<RecordedRequest> _requests = [];

    public IReadOnlyList<RecordedRequest> Requests { get { lock (_gate) return [.. _requests]; } }

    public static StubHttpMessageHandler Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(_ => Response(status, json));

    public static HttpResponseMessage Response(HttpStatusCode status, string body, string mediaType = "application/json") =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, mediaType) };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string? body = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        Dictionary<string, string> headers = request.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value), StringComparer.OrdinalIgnoreCase);

        RecordedRequest recorded = new(request.Method, request.RequestUri!, body, request.Content?.Headers.ContentType?.MediaType, headers);
        lock (_gate)
            _requests.Add(recorded);

        return responder(recorded);
    }
}
