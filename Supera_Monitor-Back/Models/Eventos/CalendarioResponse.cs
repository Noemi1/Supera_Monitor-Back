using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Models.Eventos.Aula;
using Supera_Monitor_Back.Models.Sala;

namespace Supera_Monitor_Back.Models.Eventos;

public class CalendarioResponse
{
	public List<FeriadoResponse> Feriados { get; set; }	= new List<FeriadoResponse>();
	public List<CalendarioEventoList> Eventos { get; set; }	= new List<CalendarioEventoList>();
}
