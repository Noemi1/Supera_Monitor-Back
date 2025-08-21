
namespace Supera_Monitor_Back.Services.Email;

public class ConsoleEmailService : IEmailService {
    public Task Send(string to, string subject, string html, string? from = null) {
        Console.WriteLine($"Sending email to: {to}");
        Console.WriteLine($"Subject: {subject}");
        Console.WriteLine($"Html: {html}");
        Console.WriteLine($"From: {from}");

        return Task.CompletedTask;
    }

    public Task SendEmail(string templateType, object model, string to) {
        Console.WriteLine($"Sending email to: {to}");
        Console.WriteLine($"Template type: {templateType}");
        Console.WriteLine($"Model: {model}");

        return Task.CompletedTask;
    }
}
