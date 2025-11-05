using Supera_Monitor_Back.Entities.Views;

namespace Supera_Monitor_Back.Models.Aluno;

public class SummaryModel
{
	public int Aluno_Id { get; set; }
	public int? Turma_Id { get; set; }
	public List<CalendarioAlunoList> Presencas { get; set; } = new List<CalendarioAlunoList>();
	public int Presencas_Count { get; set; }
	public List<CalendarioAlunoList> Faltas { get; set; } = new List<CalendarioAlunoList>();
	public int Faltas_Count { get; set; }
	public List<CalendarioAlunoList> Reposicoes { get; set; } = new List<CalendarioAlunoList>();
	public int Reposicoes_Count { get; set; }
	public List<CalendarioAlunoList> Aulas_Futuras { get; set; } = new List<CalendarioAlunoList>();
	public int Aulas_Futuras_Count { get; set; }
}
