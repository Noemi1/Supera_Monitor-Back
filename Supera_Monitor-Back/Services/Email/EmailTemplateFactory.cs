namespace Supera_Monitor_Back.Services.Email {
    public interface IEmailTemplateFactory {
        IEmailTemplate GetTemplate(string templateType);
    }

    public class EmailTemplateFactory : IEmailTemplateFactory {
        private readonly Dictionary<string, IEmailTemplate> _templates;

        public EmailTemplateFactory()
        {
            _templates = new Dictionary<string, IEmailTemplate> {
                { "Welcome", new WelcomeEmailTemplate() },
                { "VerifyAccount", new VerificationEmailTemplate()},
                { "ForgotPassword", new ForgotPasswordEmailTemplate() },
                { "PasswordReset", new PasswordResetEmailTemplate() }
            };
        }

        public IEmailTemplate GetTemplate(string templateType)
        {
            if (_templates.TryGetValue(templateType, out var template)) {
                return template;
            }

            throw new KeyNotFoundException($"Template {templateType} not found.");
        }
    }
}
