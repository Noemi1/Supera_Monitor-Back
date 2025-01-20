namespace Supera_Monitor_Back.Services.Email.Models {
    public class ForgotPasswordModel {
        public string? ResetToken { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
}
