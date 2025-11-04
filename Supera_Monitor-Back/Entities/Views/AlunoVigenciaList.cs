namespace Supera_Monitor_Back.Entities.Views
{
	public class AlunoVigenciaList
	{
		public int Id { get; set; }

		public int Aluno_Id { get; set; }
		
		public string Aluno { get; set; } = String.Empty;

		public int Turma_Id { get; set; }

		public string Turma { get; set; } = String.Empty;

		public int Professor_Id { get; set; }

		public string Professor { get; set; } = String.Empty;
		
		public string CorLegenda { get; set; } = String.Empty;

		public DateTime DataInicioVigencia { get; set; }
		
		public DateTime? DataFimVigencia { get; set; }
	}
}
