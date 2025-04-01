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

    public string? LoginApp { get; set; }

    public string? SenhaApp { get; set; }

    public string? RM { get; set; }

    public int? Apostila_Abaco_Id { get; set; }

    public int? Apostila_AH_Id { get; set; }

    public int? NumeroPaginaAH { get; set; }

    public int? NumeroPaginaAbaco { get; set; }

    public virtual ICollection<Aluno_Checklist_Item> Aluno_Checklist_Items { get; set; } = new List<Aluno_Checklist_Item>();

    public virtual ICollection<Aluno_Historico> Aluno_Historicos { get; set; } = new List<Aluno_Historico>();

    public virtual ICollection<Aluno_Restricao> Aluno_Restricaos { get; set; } = new List<Aluno_Restricao>();

    public virtual Apostila? Apostila_AH { get; set; }

    public virtual Apostila? Apostila_Abaco { get; set; }

    public virtual Apostila_Kit? Apostila_Kit { get; set; }

    public virtual ICollection<Aula_ListaEspera> Aula_ListaEsperas { get; set; } = new List<Aula_ListaEspera>();

    public virtual ICollection<Evento_Participacao_Aluno> Evento_Participacao_Alunos { get; set; } = new List<Evento_Participacao_Aluno>();

    public virtual Pessoa Pessoa { get; set; } = null!;

    public virtual Turma Turma { get; set; } = null!;

    public bool Active => !Deactivated.HasValue;
}