namespace Supera_Monitor_Back.Models.Dashboard
{

	public class Dashboard_Participacao
	{
		public int Id { get; set; }

		public int Aluno_Id { get; set; }

		public int Evento_Id { get; set; }

		public int? ReposicaoDe_Evento_Id { get; set; }

		public int? ReposicaoPara_Evento_Id { get; set; }

		public bool? Presente { get; set; }

		public string? Apostila_Abaco { get; set; }

		public string? Apostila_AH { get; set; }

		public int? Apostila_Abaco_Id { get; set; }

		public int? Apostila_AH_Id { get; set; }

		public int? NumeroPaginaAbaco { get; set; }

		public int? NumeroPaginaAH { get; set; }

		public string? Observacao { get; set; }

		public DateTime? Deactivated { get; set; }
	}
}
