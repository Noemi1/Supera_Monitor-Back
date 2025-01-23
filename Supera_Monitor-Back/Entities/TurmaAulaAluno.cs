namespace Supera_Monitor_Back.Entities;

public partial class TurmaAulaAluno {
    public int Id { get; set; }

    public bool? Presente { get; set; }

    public int? NumeroPaginaAbaco { get; set; }

    public int? NumeroPaginaAh { get; set; }

    public int? ApostilaAbaco { get; set; }

    public int? Ah { get; set; }

    public int Aluno_Id { get; set; }
    public virtual Aluno Aluno { get; set; } = null!;

    public int Turma_Aula_Id { get; set; }
    public virtual TurmaAula Turma_Aula { get; set; } = null!;
}
