namespace Supera_Monitor_Back.Entities;

public partial class Professor {
    public int Id { get; set; }

    public int Account_Id { get; set; }

    public int? NivelAh { get; set; }

    public int? NivelAbaco { get; set; }

    public DateTime DataInicio { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual ICollection<TurmaAula> Turma_Aulas { get; set; } = new List<TurmaAula>();

    public virtual ICollection<Turma> Turmas { get; set; } = new List<Turma>();
}
