namespace Supera_Monitor_Back.Entities
{
	public class Feriado
	{
		public int Id { get; set; }
		public DateTime Data { get; set; }
		public string Descricao { get; set; } = string.Empty;
		public int Account_Created_Id { get; set; }
		public DateTime Created { get; set; }
		public DateTime? Deactivated { get; set; }
		public Account Account_Created { get; set; } = null!;
	}
}
