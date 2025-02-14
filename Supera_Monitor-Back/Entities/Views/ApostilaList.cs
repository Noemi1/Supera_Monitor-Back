namespace Supera_Monitor_Back.Entities.Views;

public partial class ApostilaList {
    public int Id { get; set; }

    public int Apostila_Kit_Id { get; set; }

    public string Kit { get; set; } = null!;

    public string Nome { get; set; } = null!;

    public int NumeroTotalPaginas { get; set; }

    public int Ordem { get; set; }

    public string Tipo { get; set; } = null!;
}
