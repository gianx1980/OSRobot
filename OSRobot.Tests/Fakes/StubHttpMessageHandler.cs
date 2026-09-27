/*======================================================================================
    Copyright 2025 by Gianluca Di Bucci (gianx1980) (https://www.os-robot.com)

    This file is part of OSRobot.

    OSRobot is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    OSRobot is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with OSRobot.  If not, see <http://www.gnu.org/licenses/>.
======================================================================================*/

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
