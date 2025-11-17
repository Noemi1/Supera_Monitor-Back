namespace Supera_Monitor_Back.Entities.Views
{
	public class FeriadoList
	{
		public int Id { get; set; }
		public string Descricao { get; set; } = string.Empty;
		public DateTime Data { get; set; }
		public int Account_Created_Id { get; set; }
		public string Account_Created { get; set; } = string.Empty;
		public DateTime Created { get; set; }
		public DateTime? Deactivated { get; set; }
		public bool Active { get; set; }
	}
}
