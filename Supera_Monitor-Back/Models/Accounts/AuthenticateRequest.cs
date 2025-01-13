using System.ComponentModel.DataAnnotations;

namespace Supera_Monitor_Back.Models.Accounts {
    public class AuthenticateRequest {

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
