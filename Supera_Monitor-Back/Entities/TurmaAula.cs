namespace Supera_Monitor_Back.Entities;

public partial class TurmaAula {
    public int Id { get; set; }
    public DateTime Data { get; set; }
    public string? Observacao { get; set; }

    public bool? Finalizada { get; set; }

    public int Professor_Id { get; set; }
    public virtual Professor Professor { get; set; } = null!;

    public int? Turma_Id { get; set; }
    public virtual Turma? Turma { get; set; }

    public virtual ICollection<TurmaAulaAluno> Turma_Aula_Alunos { get; set; } = new List<TurmaAulaAluno>();
}
