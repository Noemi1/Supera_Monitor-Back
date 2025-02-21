namespace Supera_Monitor_Back.Entities;

public partial class Aluno_Restricao {
    public int Id { get; set; }

    public string Restricao { get; set; } = null!;

    public virtual ICollection<Aluno_Restricao_Rel> Aluno_Restricao_Rel { get; set; } = new List<Aluno_Restricao_Rel>();
}
