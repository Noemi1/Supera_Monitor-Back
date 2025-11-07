using Supera_Monitor_Back.Helpers;

namespace Supera_Monitor_Back.Models.JornadaSupera
{
	public class JornadaSuperaStatus
	{


		public static StatusChecklistItem getStatus(int NumeroSemana, DateTime Prazo, DateTime? DataFinalizacao)
		{
			DateTime hoje = TimeFunctions.HoraAtualBR();

			DateTime dataInicioPrazo = Prazo.AddDays((NumeroSemana * 6) * -1);

			if (DataFinalizacao.HasValue && DataFinalizacao.Value > Prazo )
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

			else if (dataInicioPrazo >= hoje && hoje <= Prazo)
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
		FinalizadoComAtraso,
		Finalizado,
		Atrasado,
		EmAndamento,
		ARealizar,

	}
}
