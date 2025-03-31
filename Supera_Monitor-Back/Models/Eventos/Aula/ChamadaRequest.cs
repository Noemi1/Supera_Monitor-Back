namespace Supera_Monitor_Back.Models.Eventos.Aula;

public class ChamadaRequest {
    public int Evento_Id { get; set; }
    public string? Observacao { get; set; }

    public List<ChamadaRegistroRequest> Registros { get; set; } = new();
}
