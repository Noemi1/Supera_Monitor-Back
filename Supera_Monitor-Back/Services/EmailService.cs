using Microsoft.Extensions.Options;
using Supera_Monitor_Back.Helpers;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace Supera_Monitor_Back.Services {
    public interface IEmailService {
        void Send(string to, string subject, string html, string from = null);
    }

    public class EmailService : IEmailService {
        private readonly AppSettings _appSettings;
        private readonly DataContext _db;

        public EmailService(IOptions<AppSettings> appSettings, DataContext db)
        {
            _appSettings = appSettings.Value;
            _db = db;
        }

        public void Send(string to, string subject, string html, string from = null)
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

                smtpClient.Send(email);
            } catch (Exception e) {
                Console.WriteLine(e);
            }
        }
    }
}
