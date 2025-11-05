namespace Supera_Monitor_Back.Models.Monitoramento
{
	public class Monitoramento_Aluno_Item
	{
		public int Id { get; set; }

		public bool Show { get; set; }

		public virtual Monitoramento_Aula_Participacao_Rel? Aula { get; set; } = null!;

		public virtual Monitoramento_Aula_Participacao_Rel? ReposicaoPara { get; set; }
	}
}
