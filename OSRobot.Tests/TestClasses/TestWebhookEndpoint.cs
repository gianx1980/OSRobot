// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OSRobot.Server.Configuration;
using OSRobot.Server.Controllers;
using OSRobot.Server.Core;
using OSRobot.Server.Core.Logging;
using OSRobot.Server.Core.Logging.Abstract;
using OSRobot.Server.Core.Variables;
using OSRobot.Server.Infrastructure.Webhooks;
using OSRobot.Server.JobEngineLib.Infrastructure.Abstract;
using OSRobot.Tests.Support;

namespace OSRobot.Tests.TestClasses;

/// <summary>Real HTTP calls, through a real web host and engine, on the two kinds of endpoint.</summary>
// EngineHarness uses process-wide state (Core, ProbeLog, the trigger registry).
[TestClass]
[DoNotParallelize]
public sealed class TestWebhookEndpoint
{
    private const int HookId = 9;
    private const string Token = "s3cret-token-value";

    // The engine installs its variables in Core: don't leave them to the tests that follow.
    [TestCleanup]
    public void RestoreNoVariables() => Core.Init(PluginInstanceLogger.LogPath);

    private sealed class CapturingAuditLogger : IAuditLogger
    {
        public List<string> Warnings { get; } = [];

        public void Info(string message) { }
        public void Info(string message, Exception ex) { }
        public void Error(string message) { }
        public void Error(string message, Exception ex) { }
        public void Warn(string message) { lock (Warnings) Warnings.Add(message); }
        public void Warn(string message, Exception ex) { lock (Warnings) Warnings.Add(message); }
    }

    /// <summary>The web host wired as Program.cs wires it: a main endpoint and a webhooks-only one.</summary>
    private sealed class Host : IAsyncDisposable
    {
        private readonly WebApplication _app;

        public EngineHarness Harness { get; }
        public CapturingAuditLogger Audit { get; } = new();
        public HttpClient Client { get; } = new();
        public string MainUrl { get; }
        public string WebhooksUrl { get; }

        public Host(JobGraph graph, Action<WebhooksConfig>? configure = null, IVariableResolver? variables = null)
        {
            Harness = new EngineHarness(graph, variables: variables);

            int mainPort = FreePort();
            int webhooksPort = FreePort();
            MainUrl = $"http://127.0.0.1:{mainPort}";
            WebhooksUrl = $"http://127.0.0.1:{webhooksPort}";

            WebhooksConfig config = new();
            configure?.Invoke(config);

            WebApplicationBuilder builder = WebApplication.CreateBuilder();

            // The server's appsettings.json is copied next to the tests: its Kestrel endpoints would win over UseUrls.
            builder.Configuration.Sources.Clear();
            builder.Configuration.AddInMemoryCollection();
            builder.Logging.ClearProviders();
            builder.WebHost.UseUrls(MainUrl, WebhooksUrl);
            builder.Services.AddControllers().AddApplicationPart(typeof(WebhooksController).Assembly);
            builder.Services.Configure<AppSettings>(s =>
            {
                s.Webhooks.MaxBodyBytes = config.MaxBodyBytes;
                s.Webhooks.MaxRequestsPerMinutePerIp = config.MaxRequestsPerMinutePerIp;
            });
            builder.Services.AddSingleton<IJobEngine>(Harness.Engine);
            builder.Services.AddSingleton<IAuditLogger>(Audit);
            builder.Services.AddOSRobotWebhooks(config);

            _app = builder.Build();
            _app.UseWebhooksOnlyEndpoint(webhooksPort);
            _app.UseRateLimiter();

            // Explicitly, after the restriction, as in Program.cs: otherwise the host adds it ahead of everything.
            _app.UseAuthorization();
            _app.MapControllers();
            _app.StartAsync().GetAwaiter().GetResult();
        }

        public Task<HttpResponseMessage> PostAsync(string url, string body = "{}", string? token = Token)
        {
            HttpRequestMessage request = new(HttpMethod.Post, url) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
            if (token != null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return Client.SendAsync(request);
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await _app.StopAsync();
            await _app.DisposeAsync();
            Harness.Dispose();
        }

        private static int FreePort()
        {
            TcpListener listener = new(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }

    private static object Hook(string path = "deploy", bool enabled = true, int maxCallsPerMinute = 60, int id = HookId) => new
    {
        pluginId = "WebhookEvent",
        id,
        name = "Hook",
        enabled,
        path,
        token = Token,
        maxCallsPerMinute
    };

    private static string WithoutTraceId(string problemDetails) => Regex.Replace(problemDetails, "\"traceId\":\"[^\"]*\"", string.Empty);

    private static JobGraph GraphWithHook(string reportValue = "{object[9].RequestBody}")
    {
        return new JobGraph()
            .Node(Hook())
            .Task(10, "Report", value: reportValue)
            .Connect(HookId, 10);
    }

    [TestMethod]
    public async Task A_call_with_the_token_starts_the_job_with_its_body()
    {
        await using Host host = new(GraphWithHook());

        using HttpResponseMessage response = await host.PostAsync($"{host.MainUrl}/hooks/deploy", "{\"order\":123}");

        Assert.AreEqual(HttpStatusCode.Accepted, response.StatusCode);
        Assert.IsTrue(EngineHarness.WaitFor(() => ProbeLog.Find("Report") != null, TimeSpan.FromSeconds(10)));
        Assert.AreEqual("{\"order\":123}", ProbeLog.Find("Report")!.Value);
    }

    [TestMethod]
    public async Task A_form_encoded_body_reaches_the_job_untouched()
    {
        // Regression: binding an action parameter makes MVC read the form, consuming the body before the hook
        // sees it, so a signature (GitHub can send this content type) was checked against nothing.
        await using Host host = new(GraphWithHook());

        using HttpRequestMessage request = new(HttpMethod.Post, $"{host.MainUrl}/hooks/deploy")
        {
            Content = new StringContent("a=1&b=2", Encoding.UTF8, "application/x-www-form-urlencoded")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        using HttpResponseMessage response = await host.Client.SendAsync(request);

        Assert.AreEqual(HttpStatusCode.Accepted, response.StatusCode);
        Assert.IsTrue(EngineHarness.WaitFor(() => ProbeLog.Find("Report") != null, TimeSpan.FromSeconds(10)));
        Assert.AreEqual("a=1&b=2", ProbeLog.Find("Report")!.Value);
    }

    [TestMethod]
    public async Task A_wrong_token_looks_like_an_unknown_hook_but_is_audited()
    {
        await using Host host = new(GraphWithHook());

        using HttpResponseMessage wrongToken = await host.PostAsync($"{host.MainUrl}/hooks/deploy", token: "wrong");
        using HttpResponseMessage noToken = await host.PostAsync($"{host.MainUrl}/hooks/deploy", token: null);
        using HttpResponseMessage unknownHook = await host.PostAsync($"{host.MainUrl}/hooks/unknown");

        Assert.AreEqual(HttpStatusCode.NotFound, wrongToken.StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, noToken.StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, unknownHook.StatusCode);
        Assert.AreEqual(WithoutTraceId(await unknownHook.Content.ReadAsStringAsync()), WithoutTraceId(await wrongToken.Content.ReadAsStringAsync()),
                        "Nothing tells them apart");

        Assert.HasCount(2, host.Audit.Warnings, "An unknown path is not audited: it could contain anything");
        StringAssert.Contains(host.Audit.Warnings[0], "wrong token or signature");
        Assert.IsNull(ProbeLog.Find("Report"));
    }

    [TestMethod]
    public async Task A_disabled_hook_or_a_hook_in_a_disabled_folder_does_not_exist()
    {
        JobGraph graph = new JobGraph()
            .Node(Hook("off", enabled: false))
            .Folder(20, "Paused", JobGraph.FolderContent().Node(Hook("in-paused-folder", id: 21)), enabled: false);
        await using Host host = new(graph);

        using HttpResponseMessage disabled = await host.PostAsync($"{host.MainUrl}/hooks/off");
        using HttpResponseMessage inDisabledFolder = await host.PostAsync($"{host.MainUrl}/hooks/in-paused-folder");

        Assert.AreEqual(HttpStatusCode.NotFound, disabled.StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, inDisabledFolder.StatusCode);
    }

    [TestMethod]
    public async Task A_body_over_the_limit_is_refused()
    {
        await using Host host = new(GraphWithHook(), c => c.MaxBodyBytes = 100);

        using HttpResponseMessage atLimit = await host.PostAsync($"{host.MainUrl}/hooks/deploy", new string('x', 100));
        using HttpResponseMessage overLimit = await host.PostAsync($"{host.MainUrl}/hooks/deploy", new string('x', 101));

        Assert.AreEqual(HttpStatusCode.Accepted, atLimit.StatusCode);
        Assert.AreEqual(HttpStatusCode.RequestEntityTooLarge, overLimit.StatusCode);
    }

    [TestMethod]
    public async Task The_hooks_limit_answers_429()
    {
        JobGraph graph = new JobGraph().Node(Hook(maxCallsPerMinute: 1));
        await using Host host = new(graph);

        using HttpResponseMessage first = await host.PostAsync($"{host.MainUrl}/hooks/deploy");
        using HttpResponseMessage second = await host.PostAsync($"{host.MainUrl}/hooks/deploy");

        Assert.AreEqual(HttpStatusCode.Accepted, first.StatusCode);
        Assert.AreEqual(HttpStatusCode.TooManyRequests, second.StatusCode);
    }

    [TestMethod]
    public async Task The_limit_per_address_also_stops_calls_without_a_token()
    {
        await using Host host = new(GraphWithHook(), c => c.MaxRequestsPerMinutePerIp = 2);

        using HttpResponseMessage first = await host.PostAsync($"{host.MainUrl}/hooks/unknown");
        using HttpResponseMessage second = await host.PostAsync($"{host.MainUrl}/hooks/unknown");
        using HttpResponseMessage third = await host.PostAsync($"{host.MainUrl}/hooks/unknown");

        Assert.AreEqual(HttpStatusCode.NotFound, second.StatusCode);
        Assert.AreEqual(HttpStatusCode.TooManyRequests, third.StatusCode);
    }

    [TestMethod]
    public async Task While_the_engine_is_stopped_callers_are_told_to_retry()
    {
        await using Host host = new(GraphWithHook());
        host.Harness.Stop();

        using HttpResponseMessage response = await host.PostAsync($"{host.MainUrl}/hooks/deploy");

        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [TestMethod]
    public async Task A_placeholder_in_the_body_stays_text()
    {
        // Whoever can call a hook must not be able to read a secret by naming it in the body.
        VariableSet variables = new([], [KeyValuePair.Create("Pwd", (string?)"hidden-value")]);
        await using Host host = new(GraphWithHook(), variables: variables);

        using HttpResponseMessage response = await host.PostAsync($"{host.MainUrl}/hooks/deploy", "{secret['Pwd']} {environment['PATH']}");

        Assert.AreEqual(HttpStatusCode.Accepted, response.StatusCode);
        Assert.IsTrue(EngineHarness.WaitFor(() => ProbeLog.Find("Report") != null, TimeSpan.FromSeconds(10)));
        Assert.AreEqual("{secret['Pwd']} {environment['PATH']}", ProbeLog.Find("Report")!.Value);
    }

    [TestMethod]
    public async Task A_token_kept_in_a_secret_works()
    {
        VariableSet variables = new([], [KeyValuePair.Create("HookToken", (string?)Token)]);
        JobGraph graph = new JobGraph().Node(new { pluginId = "WebhookEvent", id = HookId, name = "Hook", path = "deploy", token = "{secret['HookToken']}" });
        await using Host host = new(graph, variables: variables);

        using HttpResponseMessage right = await host.PostAsync($"{host.MainUrl}/hooks/deploy");
        using HttpResponseMessage placeholderAsToken = await host.PostAsync($"{host.MainUrl}/hooks/deploy", token: "{secret['HookToken']}");

        Assert.AreEqual(HttpStatusCode.Accepted, right.StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, placeholderAsToken.StatusCode);
    }

    // ----- The webhooks-only endpoint ------------------------------------------------------------

    [TestMethod]
    public async Task The_webhooks_only_endpoint_serves_the_hooks()
    {
        await using Host host = new(GraphWithHook());

        using HttpResponseMessage response = await host.PostAsync($"{host.WebhooksUrl}/hooks/deploy");

        Assert.AreEqual(HttpStatusCode.Accepted, response.StatusCode);
    }

    [TestMethod]
    [DataRow("GET", "/api/Robot/Objects", DisplayName = "API read")]
    [DataRow("POST", "/api/Robot/StartTask?taskId=10", DisplayName = "API start task")]
    [DataRow("POST", "/api/Account/Login", DisplayName = "Login")]
    [DataRow("GET", "/swagger/index.html", DisplayName = "Swagger")]
    [DataRow("GET", "/Public/Readme.txt", DisplayName = "Static files")]
    [DataRow("POST", "/hubs/notifications/negotiate", DisplayName = "Notifications hub")]
    [DataRow("GET", "/hooks/deploy", DisplayName = "A hook, with another method")]
    [DataRow("POST", "/hooksandmore/deploy", DisplayName = "A path only starting like the hooks")]
    [DataRow("GET", "/", DisplayName = "Root")]
    public async Task Nothing_else_exists_on_the_webhooks_only_endpoint(string method, string path)
    {
        await using Host host = new(GraphWithHook());

        using HttpRequestMessage request = new(new HttpMethod(method), host.WebhooksUrl + path);
        using HttpResponseMessage response = await host.Client.SendAsync(request);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    [DataRow("https://0.0.0.0:7099", 7099)]
    [DataRow("http://localhost:7099", 7099)]
    [DataRow("http://*:7099", 7099)]
    [DataRow("https://+:7099", 7099)]
    [DataRow("http://[::]:7099", 7099)]
    public void The_port_comes_from_the_endpoint_named_Webhooks(string url, int expectedPort)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([KeyValuePair.Create<string, string?>("Kestrel:Endpoints:Webhooks:Url", url)])
            .Build();

        Assert.AreEqual(expectedPort, WebhookEndpoint.GetWebhooksOnlyPort(configuration));
    }

    [TestMethod]
    public void Without_that_endpoint_nothing_is_restricted()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([KeyValuePair.Create<string, string?>("Kestrel:Endpoints:MyHttpEndpoint:Url", "http://localhost:7098")])
            .Build();

        Assert.IsNull(WebhookEndpoint.GetWebhooksOnlyPort(configuration));
    }

    [TestMethod]
    public void An_unusable_url_stops_the_server_instead_of_leaving_it_unrestricted()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([KeyValuePair.Create<string, string?>("Kestrel:Endpoints:Webhooks:Url", "not a url")])
            .Build();

        Assert.Throws<InvalidOperationException>(() => WebhookEndpoint.GetWebhooksOnlyPort(configuration));
    }
}
