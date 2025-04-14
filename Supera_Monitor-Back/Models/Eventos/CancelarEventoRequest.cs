namespace Supera_Monitor_Back.Models.Eventos;

public class CancelarEventoRequest {
    public int Id { get; set; }
    public string Observacao { get; set; } = string.Empty;
}
