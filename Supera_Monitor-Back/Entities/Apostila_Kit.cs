namespace Supera_Monitor_Back.Entities;

public partial class Apostila_Kit {
    public int Id { get; set; }

    public string Nome { get; set; } = null!;

    public virtual ICollection<Aluno> Alunos { get; set; } = new List<Aluno>();

    public virtual ICollection<Apostila_AH_Kit> Apostila_AH_Kits { get; set; } = new List<Apostila_AH_Kit>();

    public virtual ICollection<Apostila_Abaco_Kit> Apostila_Abaco_Kits { get; set; } = new List<Apostila_Abaco_Kit>();
}
