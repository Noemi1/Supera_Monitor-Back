using System;
using System.Collections.Generic;

namespace Supera_Monitor_Back.Scaffold;

public partial class Account
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public bool AcceptTerms { get; set; }

    public string? VerificationToken { get; set; }

    public DateTime? Verified { get; set; }

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

    public virtual ICollection<Aula_ListaEspera> Aula_ListaEsperas { get; set; } = new List<Aula_ListaEspera>();

    public virtual ICollection<Aula> Aulas { get; set; } = new List<Aula>();

    public virtual ICollection<Account> InverseAccount_Created { get; set; } = new List<Account>();

    public virtual ICollection<Log> Logs { get; set; } = new List<Log>();

    public virtual ICollection<Professor> Professors { get; set; } = new List<Professor>();

    public virtual AccountRole Role { get; set; } = null!;

    public virtual ICollection<Turma> Turmas { get; set; } = new List<Turma>();
}
