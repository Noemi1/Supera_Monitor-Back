namespace Supera_Monitor_Back.Entities;

public partial class Aluno {
    public int Id { get; set; }

    public int Pessoa_Id { get; set; }

    public int Turma_Id { get; set; }

    public string? Aluno_Foto { get; set; }

    public DateTime Created { get; set; }

    public DateTime? LastUpdated { get; set; }

    public DateTime? Deactivated { get; set; }

    public string AspNetUsers_Created_Id { get; set; } = string.Empty;

    public virtual Turma Turma { get; set; } = null!;

    public virtual ICollection<TurmaAulaAluno> Turma_Aula_Alunos { get; set; } = new List<TurmaAulaAluno>();

    public int? Apostila_Kit_Id { get; set; }

    public int? Apostila_Abaco_Id { get; set; }

    public int? Apostila_AH_Id { get; set; }

    public virtual Apostila_Kit? Apostila_Kit { get; set; }

    public bool Active => !Deactivated.HasValue;
}

