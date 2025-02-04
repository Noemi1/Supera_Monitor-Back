namespace Supera_Monitor_Back.Entities;

public partial class Aluno {
    public int Id { get; set; }

    public int Pessoa_Id { get; set; }

    public int Turma_Id { get; set; }

    public virtual Turma Turma { get; set; } = null!;

    public virtual ICollection<TurmaAulaAluno> Turma_Aula_Alunos { get; set; } = new List<TurmaAulaAluno>();
}
