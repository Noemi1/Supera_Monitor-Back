namespace Supera_Monitor_Back.Entities;

public partial class Lembrete {
    public int Id { get; set; }

    public bool Recorrente { get; set; }

    public int Duracao { get; set; }

    public int? RecorrenciaDias { get; set; }

    public string Descricao { get; set; } = null!;
}
