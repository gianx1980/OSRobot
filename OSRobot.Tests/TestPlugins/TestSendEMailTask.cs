// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Plugins.SendEMailTask;
using System.Text.Json;

namespace OSRobot.Tests.TestPlugins;

[TestClass]
public sealed class TestSendEMailTask
{
    // Mirrors the options JsonDeserialization uses when it binds a plugin config out of jobs.json.
    private static readonly JsonSerializerOptions _jobsJsonOptions = new() { PropertyNameCaseInsensitive = true };

    [TestMethod]
    public void NewTaskDefaultsToAutomaticSecurityMode()
    {
        // ---------
        // Arrange / Act
        // ---------
        SendEMailTaskConfig config = (SendEMailTaskConfig)new SendEMailTaskPlugin().GetPluginDefaultConfig();

        // ---------
        // Assert
        // ---------
        Assert.AreEqual(SendEMailSecurityMode.Auto, config.SecurityMode,
            "A newly created task should default to automatic TLS negotiation.");
    }

    [TestMethod]
    public void ConfigWithoutSecurityModeFallsBackToAutomatic()
    {
        // ---------
        // Arrange
        // ---------
        // A config that predates the setting simply has no securityMode member.
        string json = """
            {
                "id": 1,
                "name": "Send email task 1",
                "enabled": true,
                "log": true,
                "recipients": [ "someone@example.com" ],
                "sender": "robot@example.com",
                "smtpServer": "smtp.example.com",
                "port": "587",
                "authenticate": false,
                "pluginIterationMode": "IterateDefaultRecordset"
            }
            """;

        // ---------
        // Act
        // ---------
        SendEMailTaskConfig config = JsonSerializer.Deserialize<SendEMailTaskConfig>(json, _jobsJsonOptions)!;

        // ---------
        // Assert
        // ---------
        Assert.AreEqual(SendEMailSecurityMode.Auto, config.SecurityMode,
            "An absent securityMode should leave the property initializer's default in place.");
    }

    [TestMethod]
    public void ExplicitSecurityModeRoundTripsAsAString()
    {
        // ---------
        // Arrange
        // ---------
        SendEMailTaskConfig original = (SendEMailTaskConfig)new SendEMailTaskPlugin().GetPluginDefaultConfig();
        original.SecurityMode = SendEMailSecurityMode.SslOnConnect;
        original.IsBodyHtml = true;
        original.Bcc = ["hidden@example.com"];

        // ---------
        // Act
        // ---------
        string json = JsonSerializer.Serialize(original);
        SendEMailTaskConfig restored = JsonSerializer.Deserialize<SendEMailTaskConfig>(json, _jobsJsonOptions)!;

        // ---------
        // Assert
        // ---------
        // Confirms JsonStringEnumConverter is applied, so jobs.json stores "SslOnConnect" rather
        // than an opaque ordinal that would silently shift if the enum ever gets reordered.
        StringAssert.Contains(json, "SslOnConnect");
        Assert.AreEqual(SendEMailSecurityMode.SslOnConnect, restored.SecurityMode);
        Assert.IsTrue(restored.IsBodyHtml);
        Assert.AreEqual(1, restored.Bcc.Count);
        Assert.AreEqual("hidden@example.com", restored.Bcc[0]);
    }
}
