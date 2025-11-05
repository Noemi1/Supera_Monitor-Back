namespace Supera_Monitor_Back.Models.Monitoramento
{
	public class Monitoramento_Aula_Participacao_Rel
	{
		public virtual Monitoramento_Aula Aula { get; set; } = null!;
		public virtual Monitoramento_Participacao Participacao { get; set; } = null!;
	}
}
