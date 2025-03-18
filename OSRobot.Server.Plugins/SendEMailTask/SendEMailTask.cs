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
using System.Net;
using System.Net.Mail;

namespace OSRobot.Server.Plugins.SendEMailTask;

public class SendEMailTask : IterationTask
{
    protected override void RunIteration(int currentIteration)
    {
        SendEMailTaskConfig tConfig = (SendEMailTaskConfig)_iterationConfig;

        using SmtpClient mailClient = new(tConfig.SMTPServer, int.Parse(tConfig.Port));
        using MailMessage mail = new();
        mail.Sender = new MailAddress(tConfig.Sender);
        mail.From = new MailAddress(tConfig.Sender);

        foreach (string Recipient in tConfig.Recipients)
        {
            mail.To.Add(Recipient);
        }

        foreach (string ccRecipient in tConfig.CC)
        {
            mail.CC.Add(ccRecipient);
        }

        foreach (string fileAttachment in tConfig.Attachments)
        {
            mail.Attachments.Add(new Attachment(fileAttachment));
        }

        mail.Subject = tConfig.Subject;
        mail.Body = tConfig.Message;

        if (tConfig.Authenticate)
        {
            mailClient.Credentials = new NetworkCredential(tConfig.Username, tConfig.Password);
        }

        mailClient.EnableSsl = tConfig.UseSSL;
        mailClient.Port = int.Parse(tConfig.Port);
        mailClient.Send(mail);
    }
}
