namespace Supera_Monitor_Back.Entities;

public partial class Evento_Participacao_Aluno_StatusContato {
    public int Id { get; set; }

    public string Descricao { get; set; } = null!;

    public virtual ICollection<Evento_Participacao_Aluno> Evento_Participacao_Alunos { get; set; } = new List<Evento_Participacao_Aluno>();
}
