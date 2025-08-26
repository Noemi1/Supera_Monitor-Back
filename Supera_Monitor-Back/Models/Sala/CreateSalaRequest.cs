namespace Supera_Monitor_Back.Models.Sala;

public class CreateSalaRequest {
    public int NumeroSala { get; set; }
    public int Andar { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public bool? Online { get; set; }
}
