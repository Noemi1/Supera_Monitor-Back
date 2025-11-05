namespace Supera_Monitor_Back.Models.Eventos;

public class Monitoramento_Request
{
	
	public int Ano { get; set; }
	
	public int? Turma_Id { get; set; }
	
	public int? Professor_Id { get; set; }
	
	public int? Aluno_Id { get; set; }
}
