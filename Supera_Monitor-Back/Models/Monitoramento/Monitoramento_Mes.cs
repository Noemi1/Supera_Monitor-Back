namespace Supera_Monitor_Back.Models.Monitoramento
{
	public class Monitoramento_Mes
	{

		public int Mes { get; set; }
		public string MesString { get; set; } = string.Empty;
		public List<Monitoramento_Roteiro> Roteiros { get; set; } = new List<Monitoramento_Roteiro>();
	}
}
