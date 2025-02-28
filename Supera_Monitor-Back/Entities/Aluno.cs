namespace Supera_Monitor_Back.Entities;

public partial class Aluno {
    public int Id { get; set; }

    public int Pessoa_Id { get; set; }

    public int Turma_Id { get; set; }

    public string? Aluno_Foto { get; set; }

    public DateTime Created { get; set; }

    public DateTime? LastUpdated { get; set; }

    public DateTime? Deactivated { get; set; }

    public string AspNetUsers_Created_Id { get; set; } = null!;

    public int? Apostila_Kit_Id { get; set; }

    public DateTime? DataInicioVigencia { get; set; }

    public DateTime? DataFimVigencia { get; set; }

    public int? PerfilCognitivo_Id { get; set; }

    public virtual ICollection<Aluno_Checklist_Item> Aluno_Checklist_Item { get; set; } = new List<Aluno_Checklist_Item>();

    public virtual ICollection<Aluno_Restricao_Rel> Aluno_Restricao_Rel { get; set; } = new List<Aluno_Restricao_Rel>();

    public virtual Apostila_Kit? Apostila_Kit { get; set; }

    public virtual ICollection<Aula_Aluno> Aula_Aluno { get; set; } = new List<Aula_Aluno>();

    public virtual ICollection<Aula_ListaEspera> Aula_ListaEspera { get; set; } = new List<Aula_ListaEspera>();

    public virtual Turma Turma { get; set; } = null!;

    public bool Active => !Deactivated.HasValue;
}

