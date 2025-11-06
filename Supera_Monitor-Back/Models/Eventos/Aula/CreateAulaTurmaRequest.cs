namespace Supera_Monitor_Back.Models.Eventos.Aula;

public class CreateAulaTurmaRequest
{
	public DateTime Data { get; set; }
	public int Turma_Id { get; set; }
	public int Professor_Id { get; set; }
	public int Sala_Id { get; set; }
	public int DuracaoMinutos { get; set; }

	public string? Observacao { get; set; }
	public string? Descricao { get; set; }
}
