namespace Supera_Monitor_Back.Entities;

public partial class Apostila_Abaco_Kit {
    public int Id { get; set; }

    public int Apostila_Abaco_Id { get; set; }

    public int Apostila_Kit_Id { get; set; }

    public virtual Apostila_Abaco Apostila_Abaco { get; set; } = null!;

    public virtual Apostila_Kit Apostila_Kit { get; set; } = null!;

    public virtual ICollection<TurmaAulaAluno> Turma_Aula_Alunos { get; set; } = new List<TurmaAulaAluno>();
}
