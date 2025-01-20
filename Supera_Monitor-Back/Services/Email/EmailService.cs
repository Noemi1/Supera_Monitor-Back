using Microsoft.Extensions.Options;
using Supera_Monitor_Back.Helpers;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace Supera_Monitor_Back.Services.Email {
    public interface IEmailService {
        Task Send(string to, string subject, string html, string from = null);
        Task SendEmail(string templateType, object model, string to);
    }

    public class EmailService : IEmailService {
        private readonly AppSettings _appSettings;
        private readonly IEmailTemplateFactory _templateFactory;

        public EmailService(IOptions<AppSettings> appSettings, IEmailTemplateFactory templateFactory)
        {
            _appSettings = appSettings.Value;
            _templateFactory = templateFactory;
        }

        public async Task Send(string to, string subject, string html, string from = null)
        {
            try {
                // Create and populate message instance
                MailMessage email = new MailMessage();
                email.From = new MailAddress(_appSettings.SmtpUser);
                email.To.Add(to);
                email.Subject = subject;
                email.Body = html;
                email.IsBodyHtml = true;
                email.BodyEncoding = Encoding.UTF8;
                email.SubjectEncoding = Encoding.UTF8;

                // SMTP Configuration (Ex.: Gmail)
                string smtpHost = _appSettings.SmtpHost;
                int smtpPort = _appSettings.SmtpPort;
                string smtpUsername = _appSettings.SmtpUser;
                string smtpPassword = _appSettings.SmtpPass;

                // Create SMTP client
                SmtpClient smtpClient = new() {
                    Host = smtpHost,
                    Port = smtpPort,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                    EnableSsl = true
                };

                await smtpClient.SendMailAsync(email);
            } catch (Exception e) {
                Console.WriteLine(e);
            }
        }

        public async Task SendEmail(string templateType, object model, string to)
        {
            try {
                var template = _templateFactory.GetTemplate(templateType);
                string subject = template.Subject;
                string body = template.GenerateBody(model);

                MailMessage email = new() {
                    From = new MailAddress(_appSettings.SmtpUser),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                    BodyEncoding = Encoding.UTF8,
                    SubjectEncoding = Encoding.UTF8
                };

                email.To.Add(to);

                // SMTP Configuration (Ex.: Gmail)
                string smtpHost = _appSettings.SmtpHost;
                int smtpPort = _appSettings.SmtpPort;
                string smtpUsername = _appSettings.SmtpUser;
                string smtpPassword = _appSettings.SmtpPass;

                // Create SMTP client
                SmtpClient smtpClient = new() {
                    Host = smtpHost,
                    Port = smtpPort,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                    EnableSsl = true
                };

                await smtpClient.SendMailAsync(email);
            } catch (Exception e) {
                Console.WriteLine(e);
            }
        }
    }
}
