namespace Supera_Monitor_Back.Entities;

public partial class Evento_Participacao_Aluno {
    public int Id { get; set; }

    public int Evento_Id { get; set; }

    public int Aluno_Id { get; set; }

    public bool? Presente { get; set; }

    public string? Observacao { get; set; }

    public int? Apostila_AH_Id { get; set; }

    public int? Apostila_Abaco_Id { get; set; }

    public int? NumeroPaginaAH { get; set; }

    public int? NumeroPaginaAbaco { get; set; }

    public int? ReposicaoDe_Evento_Id { get; set; }

    public DateTime? Deactivated { get; set; }

    public DateTime? AlunoContactado { get; set; }

    public string? ContatoObservacao { get; set; }

    public int? StatusContato_Id { get; set; }

    public virtual Aluno Aluno { get; set; } = null!;

    public virtual Apostila? Apostila_AH { get; set; }

    public virtual Apostila? Apostila_Abaco { get; set; }

    public virtual Evento Evento { get; set; } = null!;

    public virtual Evento? ReposicaoDe_Evento { get; set; }

    public virtual Evento_Participacao_Aluno_StatusContato? StatusContato { get; set; }
}

