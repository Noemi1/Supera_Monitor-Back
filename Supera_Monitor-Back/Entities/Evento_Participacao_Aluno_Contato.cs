namespace Supera_Monitor_Back.Entities;

public partial class Evento_Participacao_Aluno_Contato {
    public int Id { get; set; }

    public DateTime Data { get; set; }

    public int Account_Id { get; set; }

    public int Evento_Participacao_Aluno_Id { get; set; }

    public string? Observacoes { get; set; }

    public bool Sucesso { get; set; }

    public int Contato_Tipo_Id { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual Evento_Participacao_Aluno_Contato_Tipo Contato_Tipo { get; set; } = null!;

    public virtual Evento_Participacao_Aluno Evento_Participacao_Aluno { get; set; } = null!;
}
