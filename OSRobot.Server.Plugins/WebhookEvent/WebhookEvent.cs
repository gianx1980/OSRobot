// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Data;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging;
using OSRobot.Server.Core.Logging.Abstract;

namespace OSRobot.Server.Plugins.WebhookEvent;

/// <summary>
/// Fires when another system calls POST /hooks/{Path}. The body is untrusted data: it reaches the job as
/// dynamic data, is never parsed for placeholders, and is never written to the logs.
/// </summary>
public class WebhookEvent : IWebhookEvent
{
    public const string RequestBody = "RequestBody";
    public const string ContentType = "ContentType";
    public const string SourceIp = "SourceIp";
    public const string RequestHeaders = "RequestHeaders";
    public const string QueryParameters = "QueryParameters";
    public const string JsonPathData = "JsonPathData";

    private const string _tokenHeader = "X-OSRobot-Token";
    private const string _authorizationHeader = "Authorization";
    private const string _bearerPrefix = "Bearer ";
    private const string _signaturePrefix = "sha256=";

    public IFolder? ParentFolder { get; set; }

    public IPluginInstanceConfig Config { get; set; } = new WebhookEventConfig();

    public List<PluginInstanceConnection> Connections { get; set; } = [];

    public string Path => ((WebhookEventConfig)Config).Path;

    public string Token => ((WebhookEventConfig)Config).Token;

    private volatile IEventSink? _sink;

    // A hook whose secret is missing logs that once: rejected calls must not be able to fill the disk.
    private int _unresolvedTokenLogged;

    // The current one-minute window of the calls-per-minute limit.
    private readonly object _rateGate = new();
    private DateTime _windowStart = DateTime.MinValue;
    private int _callsInWindow;

    public void Init(IEventSink sink)
    {
        _sink = sink;
    }

    public void Destroy()
    {
        _sink = null;
    }

    public WebhookDeliveryResult Deliver(WebhookRequest request)
    {
        IEventSink? sink = _sink;
        if (sink == null)
            return WebhookDeliveryResult.EngineNotAccepting;

        WebhookEventConfig config = (WebhookEventConfig)Config;

        if (!IsAuthorized(config, request))
            return WebhookDeliveryResult.Unauthorized;

        if (!TryTakeRateSlot(config))
            return WebhookDeliveryResult.TooManyRequests;

        // Only from here on a log file is created: rejected calls must not be able to fill the disk.
        IPluginInstanceLogger logger = PluginInstanceLogger.GetLogger(this);
        try
        {
            DynamicDataSet dDataSet;
            try
            {
                dDataSet = BuildDynamicData(config, request);
            }
            catch (Exception ex) when (ex is JsonException or ApplicationException)
            {
                logger.Error(this, $"Call from {request.SourceIp} refused: {ex.Message}");
                return WebhookDeliveryResult.BadRequest;
            }

            if (Config.Log)
            {
                logger.Info(this, $"Called from {request.SourceIp} ({request.Body.Length} bytes)");
                logger.EventTriggering(this);
            }

            return sink.Publish(this, dDataSet, logger) ? WebhookDeliveryResult.Accepted : WebhookDeliveryResult.EngineNotAccepting;
        }
        catch (Exception ex)
        {
            // Errors are always logged, regardless of Config.Log: an event has no ExecResult to report them through.
            logger.EventError(this, ex);
            throw;
        }
    }

    private bool IsAuthorized(WebhookEventConfig config, WebhookRequest request)
    {
        // Resolved at every call, so a changed secret applies at once.
        string secret;
        try
        {
            secret = DynamicDataParser.ResolveVariables(config.Token);
        }
        catch (ApplicationException ex)
        {
            if (Interlocked.Exchange(ref _unresolvedTokenLogged, 1) == 0)
                PluginInstanceLogger.GetLogger(this).Error(this, $"Calls are refused: {ex.Message}");
            return false;
        }

        // Without a token the hook is closed, never open.
        if (string.IsNullOrEmpty(secret))
            return false;

        return config.AuthMode == WebhookAuthMode.HmacSha256
            ? IsSignatureValid(secret, request.Body, GetHeader(request, config.SignatureHeader))
            : IsTokenValid(secret, request);
    }

    private static bool IsTokenValid(string secret, WebhookRequest request)
    {
        string? presented = GetHeader(request, _tokenHeader);
        if (presented == null)
        {
            string? authorization = GetHeader(request, _authorizationHeader);
            if (authorization != null && authorization.StartsWith(_bearerPrefix, StringComparison.OrdinalIgnoreCase))
                presented = authorization[_bearerPrefix.Length..].Trim();
        }

        if (presented == null)
            return false;

        // Compared as hashes in constant time: neither the content nor the length can be guessed from timing.
        return CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(secret)),
                                                       SHA256.HashData(Encoding.UTF8.GetBytes(presented)));
    }

    /// <summary>
    /// The HMAC-SHA256 of the body, in hexadecimal (GitHub) or base64 (Shopify), with or without "sha256=" in front.
    /// </summary>
    public static bool IsSignatureValid(string secret, byte[] body, string? signature)
    {
        if (string.IsNullOrWhiteSpace(signature))
            return false;

        string value = signature.Trim();
        if (value.StartsWith(_signaturePrefix, StringComparison.OrdinalIgnoreCase))
            value = value[_signaturePrefix.Length..];

        byte[] expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), body);

        byte[] presented;
        try
        {
            presented = value.Length == expected.Length * 2 ? Convert.FromHexString(value) : Convert.FromBase64String(value);
        }
        catch (FormatException)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(expected, presented);
    }

    private static string? GetHeader(WebhookRequest request, string name)
    {
        foreach (KeyValuePair<string, string> header in request.Headers)
        {
            if (header.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
                return header.Value;
        }

        return null;
    }

    private bool TryTakeRateSlot(WebhookEventConfig config)
    {
        if (config.MaxCallsPerMinute <= 0)
            return true;

        lock (_rateGate)
        {
            DateTime now = DateTime.UtcNow;
            if (now - _windowStart >= TimeSpan.FromMinutes(1))
            {
                _windowStart = now;
                _callsInWindow = 0;
            }

            if (_callsInWindow >= config.MaxCallsPerMinute)
                return false;

            _callsInWindow++;
            return true;
        }
    }

    private DynamicDataSet BuildDynamicData(WebhookEventConfig config, WebhookRequest request)
    {
        DateTime now = DateTime.Now;
        string body = Encoding.UTF8.GetString(request.Body);

        DynamicDataSet dDataSet = CommonDynamicData.BuildStandardDynamicDataSet(this, true, 0, now, now, 1);
        dDataSet.TryAdd(RequestBody, body);
        dDataSet.TryAdd(ContentType, request.ContentType);
        dDataSet.TryAdd(SourceIp, request.SourceIp);
        dDataSet.TryAdd(RequestHeaders, NameValueTable(RequestHeaders, request.Headers.Where(h => !IsCredentialHeader(config, h.Key))));
        dDataSet.TryAdd(QueryParameters, NameValueTable(QueryParameters, request.Query));

        string jsonPathData = string.Empty;
        DataTable? recordset = null;
        if (!string.IsNullOrEmpty(config.JsonPathToData))
        {
            JToken parsed = JToken.Parse(body);
            JToken? selected = parsed.SelectToken(config.JsonPathToData);
            if (selected != null)
            {
                jsonPathData = selected.ToString();
                if (config.ReturnsRecordset)
                    recordset = JsonConvert.DeserializeObject<DataTable>(jsonPathData) ?? throw new ApplicationException("The JSON data is not a list of rows.");
            }
        }

        dDataSet.TryAdd(JsonPathData, jsonPathData);
        if (config.ReturnsRecordset)
            dDataSet.TryAdd(CommonDynamicData.DefaultRecordsetName, recordset ?? new DataTable());

        return dDataSet;
    }

    // Credentials never reach the job's data, where a task could log or forward them.
    private static bool IsCredentialHeader(WebhookEventConfig config, string name)
    {
        return name.Equals(_authorizationHeader, StringComparison.OrdinalIgnoreCase)
               || name.Equals("Proxy-Authorization", StringComparison.OrdinalIgnoreCase)
               || name.Equals("Cookie", StringComparison.OrdinalIgnoreCase)
               || name.Equals(_tokenHeader, StringComparison.OrdinalIgnoreCase)
               || name.Equals(config.SignatureHeader, StringComparison.OrdinalIgnoreCase);
    }

    private static DataTable NameValueTable(string tableName, IEnumerable<KeyValuePair<string, string>> values)
    {
        DataTable table = new(tableName);
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("Value", typeof(string));

        foreach (KeyValuePair<string, string> value in values)
            table.Rows.Add(value.Key, value.Value);

        return table;
    }
}
