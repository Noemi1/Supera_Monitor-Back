using Supera_Monitor_Back.Models.Dashboard;

namespace Supera_Monitor_Back.Models.Eventos
{

	public class Dashboard_Item
	{
		public bool Show { get; set; } = false;
		public Dashboard_Aula Aula { get; set; } = null!;
		public Dashboard_Participacao Participacao { get; set; } = null!;
		public Dashboard_Roteiro Roteiro { get; set; } = null!;
		public FeriadoResponse? Feriado { get; set; } = null!;
	}
}