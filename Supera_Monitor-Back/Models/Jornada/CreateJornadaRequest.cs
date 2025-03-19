namespace Supera_Monitor_Back.Models.Jornada;

public class CreateJornadaRequest {
    public string Tema { get; set; } = null!;

    public int Semana { get; set; }

    public DateTime DataInicio { get; set; }

    public DateTime DataFim { get; set; }

    public string? CorLegenda { get; set; }
}
