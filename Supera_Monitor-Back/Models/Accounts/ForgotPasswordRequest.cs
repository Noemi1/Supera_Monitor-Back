using System.ComponentModel.DataAnnotations;

namespace Supera_Monitor_Back.Models.Accounts {
    public class ForgotPasswordRequest {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
