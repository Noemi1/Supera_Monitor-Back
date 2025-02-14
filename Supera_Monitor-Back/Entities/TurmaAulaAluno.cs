namespace Supera_Monitor_Back.Entities;

public partial class TurmaAulaAluno {
    public int Id { get; set; }

    public int Turma_Aula_Id { get; set; }

    public int Aluno_Id { get; set; }

    public bool? Reposicao { get; set; }

    public bool? Presente { get; set; }

    public int? NumeroPaginaAbaco { get; set; }

    public int? NumeroPaginaAH { get; set; }

    public int? Apostila_Abaco_Kit_Id { get; set; }

    public int? Apostila_AH_Kit_Id { get; set; }

    public virtual Aluno Aluno { get; set; } = null!;

    public virtual Apostila_AH_Kit? Apostila_AH_Kit { get; set; }

    public virtual Apostila_Abaco_Kit? Apostila_Abaco_Kit { get; set; }

    public virtual TurmaAula Turma_Aula { get; set; } = null!;
}
