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
