using System.Text.Json.Serialization;

namespace Supera_Monitor_Back.Models.Accounts {
    public class AuthenticateResponse : _BaseModel {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsVerified { get; set; }
        public DateTime? PasswordReset { get; set; }
        public string JwtToken { get; set; } = string.Empty;

        public int? Professor_Id { get; set; }

        [JsonIgnore]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
