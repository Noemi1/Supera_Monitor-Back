namespace Supera_Monitor_Back.Models.Roteiro;

public class CreateRoteiroRequest {
    public string Tema { get; set; } = null!;

    public int Semana { get; set; }

    public DateTime DataInicio { get; set; }

    public DateTime DataFim { get; set; }

    public string? CorLegenda { get; set; }
}
