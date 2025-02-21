namespace Supera_Monitor_Back.Entities;

public partial class Professor_AgendaPedagogica {
    public int Id { get; set; }

    public DateTime Data { get; set; }

    public string Descricao { get; set; } = null!;

    public virtual ICollection<Professor_AgendaPedagogica_Rel> Professor_AgendaPedagogica_Rel { get; set; } = new List<Professor_AgendaPedagogica_Rel>();
}
