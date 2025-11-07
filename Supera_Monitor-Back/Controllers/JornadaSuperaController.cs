using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Supera_Monitor_Back.Models.JornadaSupera;
using Supera_Monitor_Back.Models.JornadaSupera.Card;
using Supera_Monitor_Back.Models.JornadaSupera.List;
using Supera_Monitor_Back.Services;
using Supera_Monitor_Back.Services.Eventos;

namespace Supera_Monitor_Back.Controllers;

[ApiController]
[Route("back/jornada-supera")]
public class JornadaSuperaController : _BaseController
{
	private readonly ILogService _logger;
	private readonly IJornadaSuperaService _jornadaSuperaService;
	private readonly IEventoService _eventoService;

	public JornadaSuperaController(
		IJornadaSuperaService JornadaSuperaService,
		IEventoService eventoService,
		ILogService logger
	)
	{
		_jornadaSuperaService = JornadaSuperaService;
		_eventoService = eventoService;
		_logger = logger;
	}


	[HttpPost("cards")]
	public ActionResult<List<JornadaSupera_Card_Checklist>> GetCard(JornadaSupera_Request request)
	{
		try
		{
			var response = _jornadaSuperaService.GetCards(request);
			return Ok(response);
		}
		catch (Exception e)
		{
			_logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
			return StatusCode(500, e);
		}
	}

	[HttpPost("list")]
	public ActionResult<IEnumerable<JornadaSupera_List_Aluno>> GetList(JornadaSupera_Request request)
	{
		try
		{
			var response = _jornadaSuperaService.GetList(request);
			return Ok(response);
		}
		catch (Exception e)
		{
			_logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
			return StatusCode(500, e);
		}
	}

}
