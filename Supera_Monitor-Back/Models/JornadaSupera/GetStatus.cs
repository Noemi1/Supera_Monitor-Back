using Supera_Monitor_Back.Helpers;

namespace Supera_Monitor_Back.Models.JornadaSupera
{
	public class JornadaSuperaStatus
	{


		public static StatusChecklistItem getStatus(int NumeroSemana, DateTime Prazo, DateTime? DataFinalizacao)
		{
			DateTime hoje = TimeFunctions.HoraAtualBR().Date;

			DateTime dataInicioPrazo = x.Prazo.AddDays(-6).Date;

			if (DataFinalizacao.HasValue && DataFinalizacao.Value.Date > Prazo.Date )
			{
				return StatusChecklistItem.FinalizadoComAtraso;
			}

			else if (DataFinalizacao.HasValue)
			{
				return StatusChecklistItem.Finalizado;
			}

			else if (hoje > Prazo)
			{
				return StatusChecklistItem.Atrasado;
			}

			else if (dataInicioPrazo >= hoje && hoje <= Prazo.Date)
			{
				return StatusChecklistItem.EmAndamento;
			}

			else if (dataInicioPrazo > hoje)
			{
				return StatusChecklistItem.ARealizar;
			}

			return StatusChecklistItem.ARealizar;

		}

	}
	
	public enum StatusChecklistItem
	{
		FinalizadoComAtraso = 1,
		Finalizado = 2,
		Atrasado = 3,
		EmAndamento = 4,
		ARealizar = 5,

	}
}
