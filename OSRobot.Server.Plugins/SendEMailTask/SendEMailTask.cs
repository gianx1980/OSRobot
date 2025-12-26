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

public class SendEMailTask : MultipleIterationTask
{
    protected override void RunMultipleIterationTask(int currentIteration)
    {
        SendEMailTaskConfig config = (SendEMailTaskConfig)_iterationTaskConfig;

        using SmtpClient mailClient = new(config.SMTPServer, int.Parse(config.Port));
        using MailMessage mail = new();
        mail.Sender = new MailAddress(config.Sender);
        mail.From = new MailAddress(config.Sender);

        foreach (string Recipient in config.Recipients)
        {
            mail.To.Add(Recipient);
        }

        foreach (string ccRecipient in config.CC)
        {
            mail.CC.Add(ccRecipient);
        }

        foreach (string fileAttachment in config.Attachments)
        {
            mail.Attachments.Add(new Attachment(fileAttachment));
        }

        mail.Subject = config.Subject;
        mail.Body = config.Message;

        if (config.Authenticate)
        {
            mailClient.Credentials = new NetworkCredential(config.Username, config.Password);
        }

        mailClient.EnableSsl = config.UseSSL;
        mailClient.Port = int.Parse(config.Port);
        mailClient.Send(mail);
    }
}
