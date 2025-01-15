using System.ComponentModel.DataAnnotations;

namespace Supera_Monitor_Back.Models.Accounts {
    public class ResetPasswordRequest {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare("Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
