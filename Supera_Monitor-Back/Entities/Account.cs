namespace Supera_Monitor_Back.Entities {
    public partial class Account : BaseEntity {

        public Account()
        {
            AccountRefreshToken = new HashSet<AccountRefreshToken>();
        }

        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public bool AcceptTerms { get; set; }
        public string? VerificationToken { get; set; }
        public DateTime? Verified;
        public bool IsVerified { get; set; }
        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpires;
        public DateTime? PasswordReset;

        public virtual ICollection<AccountRefreshToken> AccountRefreshToken { get; set; }
    }
}
