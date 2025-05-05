namespace Supera_Monitor_Back.Models.Eventos.Aula;

public class UpdateAulaRequest {
    public int Id { get; set; }

    public DateTime Data { get; set; }
    public int? Turma_Id { get; set; }
    public int Roteiro_Id { get; set; }
    public int Professor_Id { get; set; }
    public int Sala_Id { get; set; }
    public int DuracaoMinutos { get; set; }
    public int CapacidadeMaximaAlunos { get; set; }

    public string? Observacao { get; set; }
    public string? Descricao { get; set; }

    public List<int> PerfilCognitivo { get; set; } = new List<int>();
}
