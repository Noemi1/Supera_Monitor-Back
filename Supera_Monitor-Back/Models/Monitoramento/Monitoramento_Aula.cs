namespace Supera_Monitor_Back.Models.Monitoramento
{
	public class Monitoramento_Aula
	{
		public int Id { get; set; }

		public int EventoTipo_Id { get; set; }
		public DateTime Data { get; set; }
		public string Descricao { get; set; } = String.Empty;
		public string Observacao { get; set; } = String.Empty;
		public bool Finalizado { get; set; }
		public DateTime? Deactivated { get; set; }
		public bool Active => !Deactivated.HasValue;

		public string Sala { get; set; } = String.Empty;
		public int Andar { get; set; }
		public int NumeroSala { get; set; }

		public bool Recesso { get; set; }
		public string Tema { get; set; } = String.Empty;
		public int Semana { get; set; }
		public string RoteiroCorLegenda { get; set; } = String.Empty;

		public int Turma_Id { get; set; }
		public string Turma { get; set; } = String.Empty;
		public string Professor { get; set; } = String.Empty;
		public string CorLegenda { get; set; } = String.Empty;

		public virtual Monitoramento_Feriado? Feriado { get; set; }
	}

}
