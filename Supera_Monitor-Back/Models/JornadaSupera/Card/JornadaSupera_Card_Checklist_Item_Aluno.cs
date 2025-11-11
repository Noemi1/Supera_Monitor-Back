
namespace Supera_Monitor_Back.Models.JornadaSupera.Card
{
	public partial class JornadaSupera_Card_Checklist_Item_Aluno
	{
		public int Id { get; set; }

		public int Aluno_Id { get; set; }

		public string Aluno { get; set; } = string.Empty;
		
		public int? Turma_Id { get; set; }

		public string? Turma { get; set; } = string.Empty;

		public string? CorLegenda { get; set; } = string.Empty;
		
		public string? Celular { get; set; } = string.Empty;

		public int NumeroSemana { get; set; }

		public DateTime Prazo { get; set; }

		public DateTime? DataFinalizacao { get; set; }

		public string? Account { get; set; }

		public int? Account_Id { get; set; }

		public string? Observacoes { get; set; }

		public bool Finalizado => DataFinalizacao != null;

		public int? Evento_Id { get; set; }

		public StatusChecklistItem Status => JornadaSuperaStatus.getStatus(NumeroSemana, Prazo, DataFinalizacao);

	}

}
