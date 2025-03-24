namespace Supera_Monitor_Back.Entities;

public partial class Aula_Aluno {
    public int Id { get; set; }

    public int Aula_Id { get; set; }

    public int Aluno_Id { get; set; }

    public bool? Presente { get; set; }

    public int? NumeroPaginaAbaco { get; set; }

    public int? NumeroPaginaAH { get; set; }

    public int? Apostila_Abaco_Id { get; set; }

    public int? Apostila_AH_Id { get; set; }

    public string? Observacao { get; set; }

    public DateTime? Deactivated { get; set; }

    public int? ReposicaoDe_Aula_Id { get; set; }

    public string? ReposicaoMotivo { get; set; }

    public virtual Aluno Aluno { get; set; } = null!;

    public virtual Apostila? Apostila_AH { get; set; }

    public virtual Apostila? Apostila_Abaco { get; set; }

    public virtual Aula Aula { get; set; } = null!;

    public virtual ICollection<Aula_Aluno_Contato> Aula_Aluno_Contato { get; set; } = new List<Aula_Aluno_Contato>();

    public virtual Aula? ReposicaoDe_Aula { get; set; }

    public bool Reposicao => ReposicaoDe_Aula_Id.HasValue;
}
