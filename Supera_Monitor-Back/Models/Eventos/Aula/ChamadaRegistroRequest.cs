namespace Supera_Monitor_Back.Models.Eventos.Aula;

public class ChamadaRegistroRequest {
    public int Participacao_Id { get; set; }
    public string? Observacao { get; set; }

    public int Apostila_Abaco_Id { get; set; }
    public int Numero_Pagina_Abaco { get; set; }

    public int Apostila_Ah_Id { get; set; }
    public int Numero_Pagina_Ah { get; set; }
}
