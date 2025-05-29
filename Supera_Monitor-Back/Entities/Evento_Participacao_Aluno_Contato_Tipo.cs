namespace Supera_Monitor_Back.Entities;

public partial class Evento_Participacao_Aluno_Contato_Tipo {
    public int Id { get; set; }

    public string Descricao { get; set; } = null!;

    public virtual ICollection<Evento_Participacao_Aluno_Contato> Evento_Participacao_Aluno_Contatos { get; set; } = new List<Evento_Participacao_Aluno_Contato>();
}
