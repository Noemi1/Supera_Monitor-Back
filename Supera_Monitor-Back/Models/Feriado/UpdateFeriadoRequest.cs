namespace Supera_Monitor_Back.Models
{
	public class UpdateFeriadoRequest
	{
		public int Id { get; set; }
		public DateTime Data { get; set; }
		public string Descricao { get; set; } = string.Empty;
	}
}
