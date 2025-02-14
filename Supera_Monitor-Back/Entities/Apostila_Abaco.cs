namespace Supera_Monitor_Back.Entities;

public partial class Apostila_Abaco {
    public int Id { get; set; }

    public string Nome { get; set; } = null!;

    public int NumeroTotalPaginas { get; set; }

    public int Ordem { get; set; }

    public virtual ICollection<Apostila_Abaco_Kit> Apostila_Abaco_Kits { get; set; } = new List<Apostila_Abaco_Kit>();
}
