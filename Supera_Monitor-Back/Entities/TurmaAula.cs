namespace Supera_Monitor_Back.Entities;

public partial class TurmaAula {
    public int Id { get; set; }
    public DateTime Data { get; set; }

    public int Professor_Id { get; set; }

    public int? Turma_Id { get; set; }
    public virtual Turma? Turma { get; set; }

    public virtual ICollection<TurmaAulaAluno> Turma_Aula_Alunos { get; set; } = new List<TurmaAulaAluno>();
}
