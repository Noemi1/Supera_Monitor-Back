namespace Supera_Monitor_Back.Entities
{
	public class Aluno_Turma_Vigencia
	{
		public int Id { get; set; }
		public int Aluno_Id { get; set; }
		public int Turma_Id { get; set; }
		public DateTime DataInicioVigencia { get; set; }
		public DateTime? DataFimVigencia { get; set; }

		public virtual Aluno Aluno { get; set; } = null!;
		public virtual Turma Turma { get; set; } = null!;
	}
}
