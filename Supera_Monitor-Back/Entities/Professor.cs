namespace Supera_Monitor_Back.Entities;

public partial class Professor {
    public int Id { get; set; }

    public DateTime DataInicio { get; set; }

    public string CorLegenda { get; set; } = string.Empty;

    public int Account_Id { get; set; }

    public virtual Account Account { get; set; } = null!;

    public int? Professor_NivelAbaco_Id { get; set; }

    public virtual Professor_NivelAbaco? Professor_NivelAbaco { get; set; }

    public int? Professor_NivelAH_Id { get; set; }

    public virtual Professor_NivelAH? Professor_NivelAH { get; set; }

    public virtual ICollection<TurmaAula> Turma_Aulas { get; set; } = new List<TurmaAula>();

    public virtual ICollection<Turma> Turmas { get; set; } = new List<Turma>();
}