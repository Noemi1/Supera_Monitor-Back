namespace Supera_Monitor_Back.Models.Monitoramento
{
	public class Monitoramento_Aluno
	{
		public int Id { get; set; }
		public string Nome { get; set; } = string.Empty;
		public string? Celular { get; set; }

		public int? Checklist_Id { get; set; }
		public int? PrimeiraAula_Id { get; set; }
		public int? AulaZero_Id { get; set; }

		public DateTime? DataNascimento { get; set; }

		public int PerfilCognitivo_Id { get; set; }
		public string? CorLegenda { get; set; }
		public string? Turma { get; set; }
		public int? Turma_Id { get; set; }
		public int? Apostila_Kit_Id { get; set; }

		public virtual List<Monitoramento_Aluno_Item> Items { get; set; } = new List<Monitoramento_Aluno_Item>();
	}

}
