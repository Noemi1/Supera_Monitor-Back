namespace Supera_Monitor_Back.Entities;

public partial class Jornada {
    public int Id { get; set; }

    public string Tema { get; set; } = null!;

    public int Semana { get; set; }

    public DateTime DataInicio { get; set; }

    public DateTime DataFim { get; set; }
}
