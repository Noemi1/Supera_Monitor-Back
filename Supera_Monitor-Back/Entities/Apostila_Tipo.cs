namespace Supera_Monitor_Back.Entities;

public partial class Apostila_Tipo {
    public int Id { get; set; }

    public string Nome { get; set; } = null!;
}

public enum ApostilaTipo {
    Abaco = 1,
    AH = 2
}