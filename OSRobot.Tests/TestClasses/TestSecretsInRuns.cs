// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.Logging;
using OSRobot.Server.Core.Variables;
using OSRobot.Tests.Support;

namespace OSRobot.Tests.TestClasses;

// Core.Variables is process-wide.
[TestClass]
[DoNotParallelize]
public sealed class TestSecretsInRuns
{
    private static readonly VariableSet _variables = new([KeyValuePair.Create("Greeting", "hello")],
                                                         [KeyValuePair.Create("Token", (string?)"abc-123-xyz")]);

    [TestCleanup]
    public void RestoreNoVariables() => Core.Init(PluginInstanceLogger.LogPath);

    [TestMethod]
    public void The_execution_log_redacts_secrets_in_messages_and_exceptions()
    {
        string logFile = Path.Combine(Path.GetTempPath(), $"OSRobotRedaction_{Guid.NewGuid():N}.log");
        Core.Init(PluginInstanceLogger.LogPath, variables: _variables);

        try
        {
            PluginInstanceLogger logger = new();
            logger.Init(logFile);
            logger.Info("calling https://api.example.com/?key=abc-123-xyz");
            logger.Error("request failed", new InvalidOperationException("401 for token abc-123-xyz"));

            string content = File.ReadAllText(logFile);
            Assert.IsFalse(content.Contains("abc-123-xyz"), content);
            StringAssert.Contains(content, "?key=***");
            StringAssert.Contains(content, "401 for token ***");
        }
        finally
        {
            File.Delete(logFile);
        }
    }

    [TestMethod]
    public void The_engine_resolves_variables_and_secrets_in_a_task()
    {
        JobGraph graph = new JobGraph()
            .Task(2, "A", value: "{var['Greeting']} {secret['Token']}")
            .Connect(JobGraph.EventId, 2);

        using EngineHarness harness = new(graph, variables: _variables);
        Assert.AreSame(_variables, Core.Variables);

        Assert.IsTrue(harness.Fire());
        Assert.IsTrue(EngineHarness.WaitFor(() => ProbeLog.Find("A") != null, TimeSpan.FromSeconds(10)));

        Assert.AreEqual("hello abc-123-xyz", ProbeLog.Find("A")!.Value);
    }
}
