namespace Supera_Monitor_Back.Entities;

public partial class Account {
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
    public int? Account_Created_Id { get; set; }
    public DateTime Created { get; set; }
    public DateTime? LastUpdated { get; set; }
    public DateTime? Deactivated { get; set; }
    public virtual ICollection<AccountRefreshToken> AccountRefreshTokens { get; set; } = new List<AccountRefreshToken>();
    public virtual Account? Account_Created { get; set; }
    public virtual ICollection<Aluno_Checklist_Item> Aluno_Checklist_Items { get; set; } = new List<Aluno_Checklist_Item>();
    public virtual ICollection<Aluno_Historico> Aluno_Historicos { get; set; } = new List<Aluno_Historico>();
    public virtual ICollection<Aluno_Restricao> Aluno_Restricaos { get; set; } = new List<Aluno_Restricao>();
    public virtual ICollection<Account> InverseAccount_Created { get; set; } = new List<Account>();
    public virtual ICollection<Log> Logs { get; set; } = new List<Log>();
    public virtual ICollection<Professor> Professors { get; set; } = new List<Professor>();
    public virtual AccountRole Role { get; set; } = null!;
    public virtual ICollection<Roteiro> Roteiros { get; set; } = new List<Roteiro>();
    public virtual ICollection<Turma> Turmas { get; set; } = new List<Turma>();

    public bool OwnsToken(string token) {
        var list = this.AccountRefreshTokens.ToList().Find(x => x.Token == token) != null;
        return list;
    }
}
