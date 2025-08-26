namespace Supera_Monitor_Back.Models.Email.Constructors;

public class ForgotPasswordModel {
    public string? ResetToken { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}
