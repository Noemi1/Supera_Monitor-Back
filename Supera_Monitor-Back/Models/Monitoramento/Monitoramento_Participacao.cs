using Supera_Monitor_Back.Entities;

namespace Supera_Monitor_Back.Models.Monitoramento
{

	public class Monitoramento_Participacao
	{
		public int Id { get; set; }

		
		public bool? Presente { get; set; }
		
		public string Observacao { get; set; } = String.Empty;
		
		public DateTime? Deactivated { get; set; }
		
		public bool Active => !Deactivated.HasValue;

		public string Apostila_Abaco { get; set; } = String.Empty;
		
		public string Apostila_AH { get; set; } = String.Empty;
		
		public int? NumeroPaginaAbaco { get; set; }
		
		public int? NumeroPaginaAH { get; set; }

		public int? Apostila_Abaco_Id { get; set; }

		public int? Apostila_AH_Id { get; set; }

		public DateTime? AlunoContactado { get; set; }
		
		public StatusContato? StatusContato_Id { get; set; }
		
		public string? ContatoObservacao { get; set; }
		
		public int? ReposicaoPara_Evento_Id { get; set; }
		
		public int? ReposicaoDe_Evento_Id { get; set; }

		public bool PrimeiraAula { get; set; }
	}

}
