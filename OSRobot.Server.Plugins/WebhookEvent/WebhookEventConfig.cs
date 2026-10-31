// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json.Serialization;
using OSRobot.Server.Core;

namespace OSRobot.Server.Plugins.WebhookEvent;

public enum WebhookAuthMode
{
    /// <summary>The caller sends the token: "Authorization: Bearer ..." or "X-OSRobot-Token: ...".</summary>
    Token,
    /// <summary>The caller signs the body with the token as key (HMAC-SHA256), as GitHub does.</summary>
    HmacSha256
}

public class WebhookEventConfig : IEventConfig
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public bool Log { get; set; } = true;

    /// <summary>The URL is /hooks/{Path}. Not the object id: pasting an object gives it a new id, which would break the callers.</summary>
    public string Path { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public WebhookAuthMode AuthMode { get; set; } = WebhookAuthMode.Token;

    /// <summary>The token or signing key, normally {secret['Name']}. While empty, every call is refused.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>With <see cref="WebhookAuthMode.HmacSha256"/>: the header holding the signature.</summary>
    public string SignatureHeader { get; set; } = "X-Hub-Signature-256";

    /// <summary>JSONPath extracting a value or array from the body, as in the REST API task.</summary>
    public string JsonPathToData { get; set; } = string.Empty;

    public bool ReturnsRecordset { get; set; }

    /// <summary>Authenticated calls accepted per minute; 0 = no limit. The task queue is unbounded: this protects it.</summary>
    public int MaxCallsPerMinute { get; set; } = 60;
}
