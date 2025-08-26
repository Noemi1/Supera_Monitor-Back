using System.ComponentModel.DataAnnotations;

namespace Supera_Monitor_Back.Models.Accounts;

public class RegisterRequest {
    [Required]
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;
    [Required]
    [Compare("Password", ErrorMessage = "Password and Confirm Password must match!")]
    public string ConfirmPassword { get; set; } = string.Empty;
    [Range(typeof(bool), "true", "true")]
    public bool AcceptTerms { get; set; }
}