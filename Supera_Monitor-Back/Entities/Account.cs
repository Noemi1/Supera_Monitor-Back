namespace Supera_Monitor_Back.Entities {
    public partial class Account : _BaseEntity {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string Email { get; set; } = null!;

        public string Phone { get; set; } = null!;

        public string PasswordHash { get; set; } = null!;

        public bool AcceptTerms { get; set; }

        public string? VerificationToken { get; set; }

        public DateTime? Verified { get; set; }
        public bool IsVerified => Verified.HasValue || PasswordReset.HasValue;
        public string? ResetToken { get; set; }

        public DateTime? ResetTokenExpires { get; set; }

        public DateTime? PasswordReset { get; set; }

        public int Role_Id { get; set; }
        public virtual AccountRole Role { get; set; } = null!;

        public virtual ICollection<Log> Log { get; set; } = new List<Log>();
        public virtual ICollection<Aula> Aula { get; set; } = new List<Aula>();
        public virtual ICollection<Turma> Turma { get; set; } = new List<Turma>();
        public virtual ICollection<Professor> Professor { get; set; } = new List<Professor>();
        public virtual ICollection<Account> Created_Account { get; set; } = new List<Account>();
        public virtual ICollection<Aula_ListaEspera> Aula_ListaEspera { get; set; } = new List<Aula_ListaEspera>();
        public virtual ICollection<AccountRefreshToken> AccountRefreshToken { get; set; } = new List<AccountRefreshToken>();

        public bool OwnsToken(string token)
        {
            var list = this.AccountRefreshToken.ToList().Find(x => x.Token == token) != null;
            return list;
        }
    }
}
