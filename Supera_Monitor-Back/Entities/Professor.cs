namespace Supera_Monitor_Back.Entities;

public partial class Professor {
    public int Id { get; set; }

    public int Account_Id { get; set; }

    public int? Professor_NivelCertificacao_Id { get; set; }

    public DateTime DataInicio { get; set; }

    public string CorLegenda { get; set; } = null!;

    public DateTime? DataNascimento { get; set; }

    public TimeSpan? ExpedienteInicio { get; set; }

    public TimeSpan? ExpedienteFim { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual ICollection<Evento_Aula> Evento_Aulas { get; set; } = new List<Evento_Aula>();

    public virtual ICollection<Evento_Participacao_Professor> Evento_Participacao_Professors { get; set; } = new List<Evento_Participacao_Professor>();

    public virtual ICollection<Professor_AgendaPedagogica_Rel> Professor_AgendaPedagogica_Rels { get; set; } = new List<Professor_AgendaPedagogica_Rel>();

    public virtual Professor_NivelCertificacao? Professor_NivelCertificacao { get; set; }

    public virtual ICollection<Turma> Turmas { get; set; } = new List<Turma>();
}
