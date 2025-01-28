namespace Supera_Monitor_Back.Entities {
    public partial class Account : _BaseEntity {
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

        public int Role_Id { get; set; }
        public virtual AccountRole Account_Role { get; set; } = null!;

        public virtual ICollection<Professor> Professors { get; set; } = new List<Professor>();

        public virtual ICollection<Account> Created_Account { get; set; } = new HashSet<Account>();
        public virtual ICollection<AccountRefreshToken> AccountRefreshToken { get; set; } = new HashSet<AccountRefreshToken>();
        public virtual ICollection<Log> Logs { get; set; } = new HashSet<Log>();

        public bool OwnsToken(string token)
        {
            var list = this.AccountRefreshToken.ToList().Find(x => x.Token == token) != null;
            return list;
        }
    }
}
