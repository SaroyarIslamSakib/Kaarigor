using CleanSolution.Domain.Enum;
using CleanSolution.Domain.Settings;
using CleanSolution.Domain.Utilities;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Text;

namespace CleanSolution.Infrastructure.Utilities
{
    public class EmailUtility : IEmailUtility
    {
        private readonly SmtpSettings _smtpSettings;
        public EmailUtility(IOptions<SmtpSettings> smtpSettings)
        {
            _smtpSettings = smtpSettings.Value;
        }
        public async Task SendEmailAsync(string receiverName, string reciverEmail, string subject, string body)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_smtpSettings.FromName, _smtpSettings.FromEmail));
            message.To.Add(new MailboxAddress(receiverName, reciverEmail));
            message.Subject = subject;
            message.Body = new TextPart("html")
            {
                Text = body
            };
            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port,
                    _smtpSettings.SmtpEncryption != SmtpEncryptionTypes.Normal);
                client.Timeout = _smtpSettings.Timeout;

                if (!string.IsNullOrEmpty(_smtpSettings.Username))
                {
                    await client.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);
                }
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }

    }
}
