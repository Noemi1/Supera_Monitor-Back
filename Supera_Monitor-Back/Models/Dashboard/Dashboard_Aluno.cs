using Supera_Monitor_Back.Models.Eventos;

namespace Supera_Monitor_Back.Models.Dashboard
{

	public class Dashboard_Aluno
	{
		public int Id { get; set; }

		public string? Nome { get; set; }

		public int? Turma_Id { get; set; }

		public string? Turma { get; set; }

		public string? CorLegenda { get; set; }

		public int? Checklist_Id { get; set; }

		public int? PrimeiraAula_Id { get; set; }

		public int? AulaZero_Id { get; set; }

		public DateTime? DataNascimento { get; set; } // Para exibir balão futuramente

		public string? Celular { get; set; }

		public List<Dashboard_Aula_Participacao> Aulas { get; set; } = new List<Dashboard_Aula_Participacao>();
	}
}
