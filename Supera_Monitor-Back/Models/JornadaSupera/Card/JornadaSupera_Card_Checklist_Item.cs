namespace Supera_Monitor_Back.Models.JornadaSupera.Card
{
	public class JornadaSupera_Card_Checklist_Item
	{
		public int Id { get; set; }
		
		public int Checklist_Id { get; set; }
		
		public string Nome { get; set; } = string.Empty;
		
		public int Ordem { get; set; }

		public List<JornadaSupera_Card_Checklist_Item_Aluno> Alunos { get; set; } = new List<JornadaSupera_Card_Checklist_Item_Aluno>() { };
	}


}
