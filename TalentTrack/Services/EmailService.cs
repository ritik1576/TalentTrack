using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TalentTrack.Models;

namespace TalentTrack.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                using var client = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort)
                {
                    Credentials = new NetworkCredential(_settings.Username, _settings.Password),
                    EnableSsl = _settings.EnableSsl
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                _logger.LogInformation("Sending email to {ToEmail}...", toEmail);
                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent successfully to {ToEmail}.", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmail}.", toEmail);
                throw;
            }
        }

        public async Task SendEmailWithAttachmentAsync(string toEmail, string subject, string body, string attachmentPath, string attachmentName)
        {
            try
            {
                using var client = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort)
                {
                    Credentials = new NetworkCredential(_settings.Username, _settings.Password),
                    EnableSsl = _settings.EnableSsl
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                if (!string.IsNullOrEmpty(attachmentPath) && System.IO.File.Exists(attachmentPath))
                {
                    var attachment = new Attachment(attachmentPath);
                    if (!string.IsNullOrEmpty(attachmentName))
                    {
                        attachment.Name = attachmentName;
                    }
                    mailMessage.Attachments.Add(attachment);
                }

                _logger.LogInformation("Sending email with attachment to {ToEmail}...", toEmail);
                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Email with attachment sent successfully to {ToEmail}.", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email with attachment to {ToEmail}.", toEmail);
                throw;
            }
        }
    }
}
