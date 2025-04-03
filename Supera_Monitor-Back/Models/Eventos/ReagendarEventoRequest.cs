namespace Supera_Monitor_Back.Models.Eventos;

public class ReagendarEventoRequest {
    public int Evento_Id { get; set; }
    public int Sala_Id { get; set; }
    public DateTime Data { get; set; }
    public string? Observacao { get; set; }
}
