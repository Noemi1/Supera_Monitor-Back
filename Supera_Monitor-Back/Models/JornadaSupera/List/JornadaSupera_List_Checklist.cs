
namespace Supera_Monitor_Back.Models.JornadaSupera.List
{
	public class JornadaSupera_List_Checklist
	{
		public int Id { get; set; }

		public string Nome { get; set; } = string.Empty;

		public int Ordem { get; set; }

		public int? NumeroSemana { get; set; }
			
		public DateTime? Prazo => getPrazo();

		public int Finalizados => countFinalizados();

		public List<JornadaSupera_List_Checklist_Item_Aluno> Items { get; set; } = new List<JornadaSupera_List_Checklist_Item_Aluno>() { };

		public StatusChecklistItem Status => getStatus();

		public StatusChecklistItem getStatus()
		{
			if (Items.Any(x => x.Status == StatusChecklistItem.Atrasado))
			{
				return StatusChecklistItem.Atrasado;
			}
			else if (Items.Any(x => x.Status == StatusChecklistItem.ARealizar))
			{
				return StatusChecklistItem.ARealizar;
			}
			else if (Items.Any(x => x.Status == StatusChecklistItem.EmAndamento))
			{
				return StatusChecklistItem.EmAndamento;
			}
			else if (Items.Any(x => x.Status == StatusChecklistItem.FinalizadoComAtraso))
			{
				return StatusChecklistItem.FinalizadoComAtraso;
			}
			else if (Items.All(x => x.Status == StatusChecklistItem.Finalizado))
			{
				return StatusChecklistItem.Finalizado;
			}

			return StatusChecklistItem.ARealizar;

		}
	
		public DateTime? getPrazo()
		{
			var first = this.Items.FirstOrDefault();
			return first?.Prazo;
		}

		public int countFinalizados()
		{
			return this.Items.Where(x => x.Finalizado == true).Count();
		}
	
	}
}
