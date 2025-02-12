namespace Supera_Monitor_Back.Entities;

public partial class TurmaAulaAluno {
    public int Id { get; set; }

    public int Turma_Aula_Id { get; set; }

    public int Aluno_Id { get; set; }

    public bool? Reposicao { get; set; }

    public bool? Presente { get; set; }

    public int? NumeroPaginaAbaco { get; set; }

    public int? NumeroPaginaAH { get; set; }

    public int? ApostilaAbaco { get; set; }

    public int? AH { get; set; }

    public virtual Aluno Aluno { get; set; } = null!;

    public virtual TurmaAula Turma_Aula { get; set; } = null!;
}
