// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Data;
using System.Security.Cryptography;
using System.Text;
using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging.Abstract;
using OSRobot.Server.Plugins.WebhookEvent;

namespace OSRobot.Tests.TestPlugins;

/// <summary>The event on its own: authentication, limits and the data it publishes. See TestWebhookEndpoint for real HTTP calls.</summary>
[TestClass]
public sealed class TestWebhookEvent
{
    private const string Token = "s3cret-token-value";

    private sealed class CapturingSink(bool accept = true) : IEventSink
    {
        public List<DynamicDataSet> Published { get; } = [];

        public bool Publish(IEvent source, DynamicDataSet dynamicData, IPluginInstanceLogger logger)
        {
            Published.Add(dynamicData);
            return accept;
        }
    }

    [TestInitialize]
    public void ConfigureLogPath() => Common.ConfigureLogPath();

    private static (WebhookEvent Event, CapturingSink Sink) Hook(Action<WebhookEventConfig>? configure = null, bool accept = true)
    {
        WebhookEventConfig config = new() { Id = 9, Name = "Hook", Path = "deploy", Token = Token };
        configure?.Invoke(config);

        WebhookEvent hook = new() { ParentFolder = Common.CreateRootFolder(), Config = config };
        CapturingSink sink = new(accept);
        hook.Init(sink);
        return (hook, sink);
    }

    private static WebhookRequest Request(string body = "{}", params (string Name, string Value)[] headers)
    {
        return new WebhookRequest("application/json", Encoding.UTF8.GetBytes(body),
                                  [.. headers.Select(h => KeyValuePair.Create(h.Name, h.Value))],
                                  [KeyValuePair.Create("source", "ci")], "192.168.1.20");
    }

    private static WebhookRequest Authorized(string body = "{}") => Request(body, ("Authorization", $"Bearer {Token}"));

    private static string HexSignature(string key, string body)
    {
        return Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(body)));
    }

    // ----- Token ---------------------------------------------------------------------------------

    [TestMethod]
    [DataRow("Authorization", "Bearer " + Token, DisplayName = "Bearer header")]
    [DataRow("authorization", "bearer " + Token, DisplayName = "Case doesn't matter")]
    [DataRow("X-OSRobot-Token", Token, DisplayName = "Own header")]
    public void The_right_token_is_accepted(string header, string value)
    {
        (WebhookEvent hook, CapturingSink sink) = Hook();

        Assert.AreEqual(WebhookDeliveryResult.Accepted, hook.Deliver(Request("{}", (header, value))));
        Assert.HasCount(1, sink.Published);
    }

    [TestMethod]
    [DataRow("Authorization", "Bearer wrong", DisplayName = "Wrong token")]
    [DataRow("Authorization", "Basic " + Token, DisplayName = "Not a bearer token")]
    [DataRow("X-Other", Token, DisplayName = "Other header")]
    public void A_wrong_token_starts_nothing(string header, string value)
    {
        (WebhookEvent hook, CapturingSink sink) = Hook();

        Assert.AreEqual(WebhookDeliveryResult.Unauthorized, hook.Deliver(Request("{}", (header, value))));
        Assert.IsEmpty(sink.Published);
    }

    [TestMethod]
    public void A_hook_without_a_token_refuses_every_call()
    {
        (WebhookEvent hook, CapturingSink sink) = Hook(c => c.Token = string.Empty);

        Assert.AreEqual(WebhookDeliveryResult.Unauthorized, hook.Deliver(Request("{}", ("Authorization", "Bearer "))));
        Assert.AreEqual(WebhookDeliveryResult.Unauthorized, hook.Deliver(Request("{}", ("X-OSRobot-Token", string.Empty))));
        Assert.IsEmpty(sink.Published);
    }

    [TestMethod]
    public void A_token_naming_a_missing_secret_refuses_every_call()
    {
        (WebhookEvent hook, CapturingSink sink) = Hook(c => c.Token = "{secret['NoSuchSecret']}");

        Assert.AreEqual(WebhookDeliveryResult.Unauthorized, hook.Deliver(Request("{}", ("X-OSRobot-Token", "{secret['NoSuchSecret']}"))));
        Assert.IsEmpty(sink.Published);
    }

    // ----- Signature -----------------------------------------------------------------------------

    [TestMethod]
    public void A_GitHub_style_signature_is_accepted()
    {
        (WebhookEvent hook, _) = Hook(c => c.AuthMode = WebhookAuthMode.HmacSha256);
        const string body = "{\"action\":\"published\"}";

        Assert.AreEqual(WebhookDeliveryResult.Accepted, hook.Deliver(Request(body, ("X-Hub-Signature-256", "sha256=" + HexSignature(Token, body)))));
    }

    [TestMethod]
    public void A_base64_signature_in_a_custom_header_is_accepted()
    {
        (WebhookEvent hook, _) = Hook(c => { c.AuthMode = WebhookAuthMode.HmacSha256; c.SignatureHeader = "X-Shopify-Hmac-Sha256"; });
        const string body = "{\"id\":1}";
        string signature = Convert.ToBase64String(HMACSHA256.HashData(Encoding.UTF8.GetBytes(Token), Encoding.UTF8.GetBytes(body)));

        Assert.AreEqual(WebhookDeliveryResult.Accepted, hook.Deliver(Request(body, ("X-Shopify-Hmac-Sha256", signature))));
    }

    [TestMethod]
    public void A_signature_of_another_body_or_key_is_refused()
    {
        (WebhookEvent hook, CapturingSink sink) = Hook(c => c.AuthMode = WebhookAuthMode.HmacSha256);

        Assert.AreEqual(WebhookDeliveryResult.Unauthorized, hook.Deliver(Request("tampered", ("X-Hub-Signature-256", "sha256=" + HexSignature(Token, "original")))));
        Assert.AreEqual(WebhookDeliveryResult.Unauthorized, hook.Deliver(Request("body", ("X-Hub-Signature-256", "sha256=" + HexSignature("other-key", "body")))));
        Assert.AreEqual(WebhookDeliveryResult.Unauthorized, hook.Deliver(Request("body", ("X-Hub-Signature-256", "not a signature!"))));
        Assert.AreEqual(WebhookDeliveryResult.Unauthorized, hook.Deliver(Request("body")));
        Assert.IsEmpty(sink.Published);
    }

    [TestMethod]
    public void In_signature_mode_the_token_itself_is_not_accepted()
    {
        (WebhookEvent hook, _) = Hook(c => c.AuthMode = WebhookAuthMode.HmacSha256);

        Assert.AreEqual(WebhookDeliveryResult.Unauthorized, hook.Deliver(Authorized()));
    }

    // ----- Limits and state -----------------------------------------------------------------------

    [TestMethod]
    public void The_calls_per_minute_limit_counts_only_authenticated_calls()
    {
        (WebhookEvent hook, CapturingSink sink) = Hook(c => c.MaxCallsPerMinute = 2);

        Assert.AreEqual(WebhookDeliveryResult.Unauthorized, hook.Deliver(Request()));
        Assert.AreEqual(WebhookDeliveryResult.Accepted, hook.Deliver(Authorized()));
        Assert.AreEqual(WebhookDeliveryResult.Unauthorized, hook.Deliver(Request()));
        Assert.AreEqual(WebhookDeliveryResult.Accepted, hook.Deliver(Authorized()));
        Assert.AreEqual(WebhookDeliveryResult.TooManyRequests, hook.Deliver(Authorized()));
        Assert.HasCount(2, sink.Published);
    }

    [TestMethod]
    public void Zero_means_no_calls_per_minute_limit()
    {
        (WebhookEvent hook, CapturingSink sink) = Hook(c => c.MaxCallsPerMinute = 0);

        for (int i = 0; i < 100; i++)
            Assert.AreEqual(WebhookDeliveryResult.Accepted, hook.Deliver(Authorized()));

        Assert.HasCount(100, sink.Published);
    }

    [TestMethod]
    public void A_hook_not_started_or_destroyed_or_an_engine_refusing_the_run_can_be_retried()
    {
        WebhookEvent notStarted = new() { Config = new WebhookEventConfig { Path = "x", Token = Token } };
        Assert.AreEqual(WebhookDeliveryResult.EngineNotAccepting, notStarted.Deliver(Authorized()));

        (WebhookEvent destroyed, _) = Hook();
        destroyed.Destroy();
        Assert.AreEqual(WebhookDeliveryResult.EngineNotAccepting, destroyed.Deliver(Authorized()));

        (WebhookEvent refused, _) = Hook(accept: false);
        Assert.AreEqual(WebhookDeliveryResult.EngineNotAccepting, refused.Deliver(Authorized()));
    }

    // ----- Published data ------------------------------------------------------------------------

    [TestMethod]
    public void The_call_is_published_without_credentials()
    {
        (WebhookEvent hook, CapturingSink sink) = Hook();
        WebhookRequest request = Request("{\"order\":123}",
                                         ("Authorization", $"Bearer {Token}"), ("Cookie", "a=b"), ("X-OSRobot-Token", Token), ("X-Request-Id", "abc"));

        Assert.AreEqual(WebhookDeliveryResult.Accepted, hook.Deliver(request));

        DynamicDataSet data = sink.Published.Single();
        Assert.AreEqual("{\"order\":123}", data[WebhookEvent.RequestBody]);
        Assert.AreEqual("application/json", data[WebhookEvent.ContentType]);
        Assert.AreEqual("192.168.1.20", data[WebhookEvent.SourceIp]);
        Assert.AreEqual(9, data[CommonDynamicData.ObjectID]);

        DataTable headers = (DataTable)data[WebhookEvent.RequestHeaders];
        Assert.AreEqual(1, headers.Rows.Count, "Only the non-credential header");
        Assert.AreEqual("X-Request-Id", headers.Rows[0]["Name"]);

        DataTable query = (DataTable)data[WebhookEvent.QueryParameters];
        Assert.AreEqual("ci", query.Rows[0]["Value"]);
    }

    [TestMethod]
    public void The_signature_header_is_not_published()
    {
        (WebhookEvent hook, CapturingSink sink) = Hook(c => c.AuthMode = WebhookAuthMode.HmacSha256);

        hook.Deliver(Request("x", ("X-Hub-Signature-256", "sha256=" + HexSignature(Token, "x")), ("X-GitHub-Event", "push")));

        DataTable headers = (DataTable)sink.Published.Single()[WebhookEvent.RequestHeaders];
        Assert.AreEqual("X-GitHub-Event", headers.Rows.Cast<DataRow>().Single()["Name"]);
    }

    [TestMethod]
    public void A_json_path_extracts_a_value()
    {
        (WebhookEvent hook, CapturingSink sink) = Hook(c => c.JsonPathToData = "$.order.id");

        hook.Deliver(Authorized("{\"order\":{\"id\":123}}"));

        Assert.AreEqual("123", sink.Published.Single()[WebhookEvent.JsonPathData]);
    }

    [TestMethod]
    public void A_json_path_can_return_a_recordset()
    {
        (WebhookEvent hook, CapturingSink sink) = Hook(c => { c.JsonPathToData = "$.items"; c.ReturnsRecordset = true; });

        hook.Deliver(Authorized("{\"items\":[{\"sku\":\"A\"},{\"sku\":\"B\"}]}"));

        DataTable rows = (DataTable)sink.Published.Single()[CommonDynamicData.DefaultRecordsetName];
        Assert.AreEqual(2, rows.Rows.Count);
        Assert.AreEqual("B", rows.Rows[1]["sku"]);
    }

    [TestMethod]
    public void A_json_path_finding_nothing_gives_empty_data()
    {
        (WebhookEvent hook, CapturingSink sink) = Hook(c => { c.JsonPathToData = "$.missing"; c.ReturnsRecordset = true; });

        Assert.AreEqual(WebhookDeliveryResult.Accepted, hook.Deliver(Authorized()));

        DynamicDataSet data = sink.Published.Single();
        Assert.AreEqual(string.Empty, data[WebhookEvent.JsonPathData]);
        Assert.AreEqual(0, ((DataTable)data[CommonDynamicData.DefaultRecordsetName]).Rows.Count);
    }

    [TestMethod]
    public void A_body_that_is_not_json_is_refused_when_a_json_path_is_set()
    {
        (WebhookEvent hook, CapturingSink sink) = Hook(c => c.JsonPathToData = "$.a");

        Assert.AreEqual(WebhookDeliveryResult.BadRequest, hook.Deliver(Authorized("not json")));
        Assert.IsEmpty(sink.Published);
    }

    [TestMethod]
    public void Without_a_json_path_any_text_is_accepted()
    {
        (WebhookEvent hook, CapturingSink sink) = Hook();

        Assert.AreEqual(WebhookDeliveryResult.Accepted, hook.Deliver(Authorized("not json")));
        Assert.AreEqual("not json", sink.Published.Single()[WebhookEvent.RequestBody]);
    }
}
