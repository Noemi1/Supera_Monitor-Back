namespace Supera_Monitor_Back.Entities;

public partial class Apostila_AH {
    public int Id { get; set; }

    public string Nome { get; set; } = null!;

    public int NumeroTotalPaginas { get; set; }

    public int Ordem { get; set; }

    public virtual ICollection<Apostila_AH_Kit> Apostila_AH_Kits { get; set; } = new List<Apostila_AH_Kit>();
}
