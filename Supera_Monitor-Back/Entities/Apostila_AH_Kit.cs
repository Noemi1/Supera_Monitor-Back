namespace Supera_Monitor_Back.Entities;

public partial class Apostila_AH_Kit {
    public int Id { get; set; }

    public int Apostila_AH_Id { get; set; }

    public int Apostila_Kit_Id { get; set; }

    public virtual Apostila_AH Apostila_AH { get; set; } = null!;

    public virtual Apostila_Kit Apostila_Kit { get; set; } = null!;

    public virtual ICollection<TurmaAulaAluno> Turma_Aula_Alunos { get; set; } = new List<TurmaAulaAluno>();
}
