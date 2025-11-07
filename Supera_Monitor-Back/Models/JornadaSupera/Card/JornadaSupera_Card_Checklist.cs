namespace Supera_Monitor_Back.Models.JornadaSupera.Card
{
	public class JornadaSupera_Card_Checklist
	{
		public int Id { get; set; }
		
		public string Nome { get; set; } = string.Empty;

		public int Ordem { get; set; }
		
		public int? NumeroSemana { get; set; }

		public List<JornadaSupera_Card_Checklist_Item> Items { get; set; } = new List<JornadaSupera_Card_Checklist_Item>() { };

	}
}
