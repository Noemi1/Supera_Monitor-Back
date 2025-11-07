namespace Supera_Monitor_Back.Models.JornadaSupera.List
{
	public class JornadaSupera_List_Checklist_Item_Aluno
	{
		public int Id { get; set; }

		public int Checklist_Item_Id { get; set; }
		
		public string Checklist_Item { get; set; } = string.Empty;

		public int Aluno_Id { get; set; }

		public int NumeroSemana { get; set; }

		public DateTime Prazo { get; set; }

		public DateTime? DataFinalizacao { get; set; }

		public string? Account { get; set; }

		public int? Account_Id { get; set; }

		public string? Observacoes { get; set; }

		public StatusChecklistItem Status => JornadaSuperaStatus.getStatus(NumeroSemana, Prazo, DataFinalizacao);

	}
}
