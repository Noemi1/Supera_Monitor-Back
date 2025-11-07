namespace Supera_Monitor_Back.Models.JornadaSupera.List
{
	public class JornadaSupera_List_Aluno
	{
		public int Id { get; set; }

		public string Nome { get; set; } = string.Empty;

		public int? Turma_Id { get; set; }

		public string? Turma { get; set; } = string.Empty;

		public string? CorLegenda { get; set; } = string.Empty;

		public string? Celular { get; set; } = string.Empty;

		public List<JornadaSupera_List_Checklist> Checklists { get; set; } = new List<JornadaSupera_List_Checklist>() { };
	}
}
