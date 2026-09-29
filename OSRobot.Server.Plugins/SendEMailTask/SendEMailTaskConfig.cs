// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Persistence;
using System.Text.Json.Serialization;

namespace OSRobot.Server.Plugins.SendEMailTask;

/// <summary>How the SMTP connection is secured. Mirrors MailKit's SecureSocketOptions.</summary>
public enum SendEMailSecurityMode
{
    /// <summary>Plain, unencrypted connection.</summary>
    None,
    /// <summary>Let the client decide: implicit TLS on port 465, otherwise STARTTLS if offered.</summary>
    Auto,
    /// <summary>Require STARTTLS; fail if the server doesn't offer it.</summary>
    StartTls,
    /// <summary>Use STARTTLS when the server offers it, otherwise continue unencrypted.</summary>
    StartTlsWhenAvailable,
    /// <summary>Implicit TLS from the first byte, as used on port 465 (SMTPS).</summary>
    SslOnConnect
}

public class SendEMailTaskConfig : ITaskConfig
{
    public const int _defaultSMTPPort = 25;

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public bool Log { get; set; } = true;

    [DynamicData]
    public List<string> Recipients { get; set; } = [];
    
    [DynamicData]
    public List<string> CC { get; set; } = [];

    [DynamicData]
    public List<string> Bcc { get; set; } = [];

    [DynamicData]
    public string Subject { get; set; } = string.Empty;

    [DynamicData]
    public string Message { get; set; } = string.Empty;

    /// <summary>Send the message body as HTML instead of plain text.</summary>
    public bool IsBodyHtml { get; set; }

    [DynamicData]
    public List<string> Attachments { get; set; } = [];

    [DynamicData]
    public string Sender { get; set; } = string.Empty;

    [DynamicData]
    [XmlEncryptField]
    public string SMTPServer { get; set; } = string.Empty;

    [DynamicData]
    public string Port { get; set; } = _defaultSMTPPort.ToString();

    /// <summary>How the connection to the SMTP server is secured.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SendEMailSecurityMode SecurityMode { get; set; } = SendEMailSecurityMode.Auto;

    public bool Authenticate { get; set; }

    [DynamicData]
    [XmlEncryptField]
    public string Username { get; set; } = string.Empty;
    [DynamicData]
    [XmlEncryptField]
    public string Password { get; set; } = string.Empty;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public IterationMode PluginIterationMode { get; set; }
    public string IterationObject { get; set; } = string.Empty;
    public int IterationsCount { get; set; }
}
