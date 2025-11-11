namespace Supera_Monitor_Back.Models.Eventos;

public class UpdateEventoRequest
{
	public int Id { get; set; }

	public string Descricao { get; set; } = string.Empty;
	public string? Observacao { get; set; }
	public int DuracaoMinutos { get; set; }
	public int Sala_Id { get; set; }
	public int? Turma_Id { get; set; }
	public int? Roteiro_Id { get; set; }
	public int CapacidadeMaximaAlunos { get; set; }

	public List<int> Professores { get; set; } = new();
	public List<int> PerfilCognitivo { get; set; } = new List<int>();
}
