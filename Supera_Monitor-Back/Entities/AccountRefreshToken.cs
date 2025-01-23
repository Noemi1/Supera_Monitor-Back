using Supera_Monitor_Back.Helpers;

namespace Supera_Monitor_Back.Entities {
    public partial class AccountRefreshToken {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime Expires { get; set; }
        public bool IsExpired => TimeFunctions.HoraAtualBR() >= Expires;
        public DateTime Created { get; set; }
        public string? CreatedByIp { get; set; }
        public DateTime? Revoked { get; set; }
        public string? RevokedByIp { get; set; }
        public string? ReplacedByToken { get; set; }
        public bool IsActive => Revoked == null && !IsExpired;
        public virtual Account Account { get; set; } = null!;
    }
}
