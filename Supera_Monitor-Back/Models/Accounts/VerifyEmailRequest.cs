using System.ComponentModel.DataAnnotations;

namespace Supera_Monitor_Back.Models.Accounts {
    public class VerifyEmailRequest {
        [Required]
        public string Token { get; set; } = string.Empty;
    }
}
