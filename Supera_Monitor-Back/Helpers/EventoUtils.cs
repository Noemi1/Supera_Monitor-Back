using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Models;

namespace Supera_Monitor_Back.Helpers
{
	public class EventoUtils
	{
		public static ResponseModel ValidateEvent(Evento? evento)
		{
			if (evento is null)
				return new ResponseModel { Message = "Evento não encontrado." };

			if (evento.Deactivated.HasValue)
				return new ResponseModel { Message = $"Este evento foi cancelado às {evento.Deactivated.Value:g}" };

			if (evento.Finalizado)
				return new ResponseModel { Message = "Evento já está finalizado" };

			return new ResponseModel { Success = true, Message = "Evento válido" };
		}
	}
}
