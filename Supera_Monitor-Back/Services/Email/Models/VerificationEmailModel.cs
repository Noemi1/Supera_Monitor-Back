namespace Supera_Monitor_Back.Services.Email.Models {
    public class VerificationEmailModel {
        public string? VerificationToken { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RandomPassword { get; set; } = string.Empty;
    }
}
