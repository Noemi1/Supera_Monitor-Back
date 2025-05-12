namespace Supera_Monitor_Back.Models.Eventos;

public class NewEventoRequest {
    public DateTime Data { get; set; }
    public string? Descricao { get; set; }
    public string? Observacao { get; set; }
    public int DuracaoMinutos { get; set; }
    public int CapacidadeMaximaAlunos { get; set; }

    public int Evento_Tipo_Id { get; set; }
    public int Sala_Id { get; set; }

    // Relacionados à Evento_Aula - Aula, Aula Zero
    public int? Turma_Id { get; set; }
    public List<int> PerfilCognitivo { get; set; } = new();

    public int? ReagendamentoDe_Evento_Id { get; set; }

    // Participantes do evento
    public List<int> Professores { get; set; } = new();
    public List<int> Alunos { get; set; } = new();
}
