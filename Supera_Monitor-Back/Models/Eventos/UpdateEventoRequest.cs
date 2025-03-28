namespace Supera_Monitor_Back.Models.Eventos;

public class UpdateEventoRequest {
    public int Id { get; set; }

    public DateTime Data { get; set; }
    public string? Descricao { get; set; }
    public string? Observacao { get; set; }
    public int DuracaoMinutos { get; set; }
    public int Sala_Id { get; set; }

    public List<int> Professores { get; set; } = new();
    public List<int> Alunos { get; set; } = new();
}
