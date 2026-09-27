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

using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using OSRobot.Server.Core;
using System.Text;

namespace OSRobot.Server.Plugins.SendEMailTask;

public class SendEMailTask : MultipleIterationTask
{
    protected override async Task RunMultipleIterationTaskAsync(int currentIteration)
    {
        SendEMailTaskConfig config = (SendEMailTaskConfig)_iterationTaskConfig;

        if (!int.TryParse(config.Port, out int port) || port <= 0 || port > 65535)
            throw new ApplicationException($"'{config.Port}' is not a valid SMTP port.");

        using MimeMessage mail = await BuildMessageAsync(config);

        // The transcript is kept in memory and only written to the task log, never to stdout.
        // RedactSecrets keeps the AUTH exchange (i.e. the password) out of it.
        using MemoryStream protocolLog = new();
        using ProtocolLogger protocolLogger = new(protocolLog, true) { RedactSecrets = true };
        using SmtpClient mailClient = new(protocolLogger);

        try
        {
            await mailClient.ConnectAsync(config.SMTPServer, port, ResolveSecurityMode(config), _cancellationToken);

            if (config.Authenticate)
                await mailClient.AuthenticateAsync(config.Username, config.Password, _cancellationToken);

            await mailClient.SendAsync(mail, _cancellationToken);
            await mailClient.DisconnectAsync(true, _cancellationToken);

            if (Config.Log)
                LogProtocolTranscript(protocolLog);
        }
        catch
        {
            // The SMTP transcript is usually the only thing that explains a delivery failure, so
            // it is logged even when Config.Log is off - it's error detail, not verbose output.
            LogProtocolTranscript(protocolLog);
            throw;
        }
    }

    private static SecureSocketOptions ResolveSecurityMode(SendEMailTaskConfig config)
        => config.SecurityMode switch
        {
            SendEMailSecurityMode.None => SecureSocketOptions.None,
            SendEMailSecurityMode.StartTls => SecureSocketOptions.StartTls,
            SendEMailSecurityMode.StartTlsWhenAvailable => SecureSocketOptions.StartTlsWhenAvailable,
            SendEMailSecurityMode.SslOnConnect => SecureSocketOptions.SslOnConnect,
            _ => SecureSocketOptions.Auto
        };

    private static async Task<MimeMessage> BuildMessageAsync(SendEMailTaskConfig config)
    {
        MimeMessage mail = new();
        mail.From.Add(MailboxAddress.Parse(config.Sender));

        AddAddresses(mail.To, config.Recipients);
        AddAddresses(mail.Cc, config.CC);
        AddAddresses(mail.Bcc, config.Bcc);

        if (mail.To.Count == 0 && mail.Cc.Count == 0 && mail.Bcc.Count == 0)
            throw new ApplicationException("The email has no recipients.");

        mail.Subject = config.Subject;

        BodyBuilder bodyBuilder = new();
        if (config.IsBodyHtml)
            bodyBuilder.HtmlBody = config.Message;
        else
            bodyBuilder.TextBody = config.Message;

        foreach (string fileAttachment in config.Attachments)
        {
            if (!string.IsNullOrWhiteSpace(fileAttachment))
                await bodyBuilder.Attachments.AddAsync(fileAttachment);
        }

        mail.Body = bodyBuilder.ToMessageBody();
        return mail;
    }

    private static void AddAddresses(InternetAddressList target, List<string> addresses)
    {
        foreach (string address in addresses)
        {
            // A blank entry is a UI artifact or dynamic data that resolved to nothing, not a
            // failure. If that leaves no recipients at all, BuildMessageAsync throws.
            if (!string.IsNullOrWhiteSpace(address))
                target.Add(MailboxAddress.Parse(address));
        }
    }

    private void LogProtocolTranscript(MemoryStream protocolLog)
    {
        string transcript = Encoding.UTF8.GetString(protocolLog.ToArray()).Trim();
        if (transcript.Length > 0)
            _instanceLogger?.Info(this, $"SMTP transcript:{Environment.NewLine}{transcript}");
    }
}
