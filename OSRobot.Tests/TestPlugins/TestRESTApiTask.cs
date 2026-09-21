using System.Data;
using System.Net;
using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging.Abstract;
using OSRobot.Server.Plugins.Infrastructure.Network;
using OSRobot.Server.Plugins.RESTApiTask;
using OSRobot.Tests.Fakes;

namespace OSRobot.Tests.TestPlugins;

/// <summary>
/// RESTApiTask tests against a stub HTTP handler (see StubHttpMessageHandler): no socket is opened, so
/// https:// URLs work without DNS, certificates or connectivity. The real-API version lives in IntegrationTests.
/// </summary>
[TestClass]
public sealed class TestRESTApiTask
{
    private const string PostsJson = """
        [
          { "userId": 1, "id": 1, "title": "first" },
          { "userId": 1, "id": 2, "title": "second" },
          { "userId": 2, "id": 3, "title": "third" }
        ]
        """;

    private static async Task<InstanceExecResult> RunAsync(RESTApiTaskConfig config, StubHttpMessageHandler handler)
    {
        using IDisposable scope = PluginServices.Override(httpHandler: handler);

        RESTApiTask task = new() { ParentFolder = Common.CreateRootFolder(), Config = config };

        Common.ConfigureLogPath();
        (DynamicDataChain chain, DynamicDataSet dataSet, IPluginInstanceLogger logger) = Common.GetTaskDefaultParameters(task);

        task.Init();
        try
        {
            return await task.RunAsync(chain, dataSet, 0, logger, CancellationToken.None);
        }
        finally
        {
            task.Destroy();
        }
    }

    private static RESTApiTaskConfig NewConfig(MethodType method, string url = "https://api.example.com/posts") => new()
    {
        Id = 1,
        Name = "REST Api task 1",
        URL = url,
        Method = method
    };

    [TestMethod]
    public async Task Get_over_https_returns_the_json_as_a_recordset()
    {
        StubHttpMessageHandler handler = StubHttpMessageHandler.Json(PostsJson);
        RESTApiTaskConfig config = NewConfig(MethodType.Get);
        config.JsonPathToData = "$";
        config.ReturnsRecordset = true;

        InstanceExecResult result = await RunAsync(config, handler);

        ExecResult execResult = result.ExecResults[0];
        Assert.IsTrue(execResult.Result, "Task failed.");

        DataTable data = (DataTable)execResult.Data["DefaultRecordset"];
        Assert.AreEqual(3, data.Rows.Count);
        Assert.AreEqual("second", data.Rows[1]["title"]);

        Assert.HasCount(1, handler.Requests);
        Assert.AreEqual(HttpMethod.Get, handler.Requests[0].Method);
        Assert.AreEqual("https://api.example.com/posts", handler.Requests[0].Uri.ToString());
    }

    [TestMethod]
    public async Task JsonPath_extracts_a_value_into_the_dynamic_data()
    {
        StubHttpMessageHandler handler = StubHttpMessageHandler.Json("""{ "data": { "token": "abc123" } }""");
        RESTApiTaskConfig config = NewConfig(MethodType.Get);
        config.JsonPathToData = "$.data.token";

        ExecResult execResult = (await RunAsync(config, handler)).ExecResults[0];

        Assert.IsTrue(execResult.Result, "Task failed.");
        Assert.AreEqual("abc123", execResult.Data["JsonPathData"]);
        Assert.AreEqual("200", execResult.Data["HttpResult"]);
    }

    [TestMethod]
    public async Task Post_sends_the_body_as_json_with_the_configured_headers()
    {
        StubHttpMessageHandler handler = StubHttpMessageHandler.Json("""{ "id": 101 }""", HttpStatusCode.Created);
        RESTApiTaskConfig config = NewConfig(MethodType.Post);
        config.Body = """{ "title": "hello" }""";
        config.Headers.Add(new RESTApiHeader { Name = "X-Api-Key", Value = "secret-key" });

        ExecResult execResult = (await RunAsync(config, handler)).ExecResults[0];

        Assert.IsTrue(execResult.Result, "Task failed.");
        Assert.AreEqual("201", execResult.Data["HttpResult"]);

        RecordedRequest request = handler.Requests.Single();
        Assert.AreEqual(HttpMethod.Post, request.Method);
        Assert.AreEqual("""{ "title": "hello" }""", request.Body);
        Assert.AreEqual("application/json", request.ContentType);
        Assert.AreEqual("secret-key", request.Headers["X-Api-Key"]);
    }

    [TestMethod]
    public async Task Put_and_delete_use_the_matching_http_method()
    {
        StubHttpMessageHandler putHandler = StubHttpMessageHandler.Json("{}");
        RESTApiTaskConfig putConfig = NewConfig(MethodType.Put);
        putConfig.Body = "{}";
        await RunAsync(putConfig, putHandler);
        Assert.AreEqual(HttpMethod.Put, putHandler.Requests.Single().Method);

        StubHttpMessageHandler deleteHandler = StubHttpMessageHandler.Json("{}");
        await RunAsync(NewConfig(MethodType.Delete), deleteHandler);
        Assert.AreEqual(HttpMethod.Delete, deleteHandler.Requests.Single().Method);
    }

    [TestMethod]
    [DataRow(HttpStatusCode.NotFound)]
    [DataRow(HttpStatusCode.InternalServerError)]
    [DataRow(HttpStatusCode.Unauthorized)]
    public async Task A_non_success_status_fails_the_task(HttpStatusCode status)
    {
        StubHttpMessageHandler handler = StubHttpMessageHandler.Json("""{ "error": "nope" }""", status);

        ExecResult execResult = (await RunAsync(NewConfig(MethodType.Get), handler)).ExecResults[0];

        Assert.IsFalse(execResult.Result, $"HTTP {(int)status} should fail the task.");
    }

    [TestMethod]
    public async Task A_network_failure_fails_the_task()
    {
        StubHttpMessageHandler handler = new(_ => throw new HttpRequestException("Name or service not known"));

        ExecResult execResult = (await RunAsync(NewConfig(MethodType.Get), handler)).ExecResults[0];

        Assert.IsFalse(execResult.Result, "A connection error should fail the task.");
    }

    [TestMethod]
    public async Task A_response_that_is_not_json_fails_when_a_json_path_is_requested()
    {
        StubHttpMessageHandler handler = new(_ => StubHttpMessageHandler.Response(HttpStatusCode.OK, "<html>not json</html>", "text/html"));
        RESTApiTaskConfig config = NewConfig(MethodType.Get);
        config.JsonPathToData = "$.anything";

        ExecResult execResult = (await RunAsync(config, handler)).ExecResults[0];

        Assert.IsFalse(execResult.Result, "Unparseable JSON should fail the task.");
    }
}
