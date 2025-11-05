namespace Supera_Monitor_Back.Models.Monitoramento
{
	public class Monitoramento_Roteiro
	{
		public int Id { get; set; }
	
		public string Tema { get; set; } = string.Empty;
		
		public int Semana { get; set; }
		
		public DateTime DataInicio { get; set; }
		
		public DateTime DataFim { get; set; }
		
		public string CorLegenda { get; set; } = string.Empty;
		
		public bool Recesso { get; set; }
	}

}
