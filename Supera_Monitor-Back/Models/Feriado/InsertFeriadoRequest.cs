namespace Supera_Monitor_Back.Models.Feriado
{
	public class InsertFeriadoRequest
	{
		public DateTime Data { get; set; }
		public string Descricao { get; set; } = string.Empty;
	}
}
