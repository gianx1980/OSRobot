// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging.Abstract;
using OSRobot.Server.Plugins.RESTApiTask;
using OSRobot.Tests.Fakes;

namespace OSRobot.Tests.TestPlugins;

/// <summary>
/// RESTApiTask against a real local HTTPS server with a self-signed (untrusted) certificate, without
/// the stub handler, so the actual certificate validation runs.
/// </summary>
[TestClass]
public sealed class TestRESTApiTaskCertificates
{
    private const string TokenJson = """{ "token": "abc123" }""";

    private static async Task<ExecResult> PostAsync(string url, bool ignoreCertificateErrors)
    {
        RESTApiTaskConfig config = new()
        {
            Id = 1,
            Name = "Get token",
            URL = url,
            Method = MethodType.Post,
            Body = """{ "user": "u" }""",
            IgnoreCertificateErrors = ignoreCertificateErrors
        };
        RESTApiTask task = new() { ParentFolder = Common.CreateRootFolder(), Config = config };

        Common.ConfigureLogPath();
        (DynamicDataChain chain, DynamicDataSet dataSet, IPluginInstanceLogger logger) = Common.GetTaskDefaultParameters(task);

        InstanceExecResult result = await task.RunAsync(chain, dataSet, 0, logger, CancellationToken.None);
        return result.ExecResults.Single();
    }

    [TestMethod]
    public async Task Untrusted_certificate_fails_by_default()
    {
        using SelfSignedHttpsServer server = new(TokenJson);

        ExecResult result = await PostAsync(server.Url, ignoreCertificateErrors: false);

        Assert.IsFalse(result.Result);
    }

    [TestMethod]
    public async Task Untrusted_certificate_is_accepted_when_certificate_errors_are_ignored()
    {
        using SelfSignedHttpsServer server = new(TokenJson);

        ExecResult result = await PostAsync(server.Url, ignoreCertificateErrors: true);

        Assert.IsTrue(result.Result);
        Assert.AreEqual(TokenJson, result.Data["RawContent"]);
    }
}
