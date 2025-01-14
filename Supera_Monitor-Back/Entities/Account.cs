namespace Supera_Monitor_Back.Entities {
    public partial class Account : BaseEntity {

        public Account()
        {
            Created_Account = new HashSet<Account>();
            AccountRefreshToken = new HashSet<AccountRefreshToken>();
        }

        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public bool AcceptTerms { get; set; }
        public string? VerificationToken { get; set; }
        public DateTime? Verified { get; set; }
        public bool IsVerified => Verified.HasValue || PasswordReset.HasValue;
        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpires { get; set; }
        public DateTime? PasswordReset { get; set; }

        public int? Role_Id { get; set; }
        public AccountRole? AccountRole { get; set; } = null!;

        public virtual ICollection<Account> Created_Account { get; set; }

        public virtual ICollection<AccountRefreshToken> AccountRefreshToken { get; set; }

    }
}
