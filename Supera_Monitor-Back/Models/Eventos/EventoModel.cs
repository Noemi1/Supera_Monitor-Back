namespace Supera_Monitor_Back.Models.Eventos;

public class EventoModel {
    public int Id { get; set; }

    public DateTime Data { get; set; }

    public int Sala_Id { get; set; }

    public string Sala { get; set; } = null!;

    public string Descricao { get; set; } = null!;

    public string? Observacao { get; set; }

    public bool Finalizado { get; set; }

    public int Account_Created_Id { get; set; }

    public DateTime Created { get; set; }

    public DateTime? LastUpdated { get; set; }

    public DateTime? Deactivated { get; set; }

    public int? ReagendamentoDe_Evento_Id { get; set; }

    public int Evento_Tipo_Id { get; set; }

    public string Evento_Tipo { get; set; } = null!;
}
