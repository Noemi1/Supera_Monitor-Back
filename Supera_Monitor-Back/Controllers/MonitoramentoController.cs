using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Supera_Monitor_Back.Models.Eventos;
using Supera_Monitor_Back.Models.Monitoramento;
using Supera_Monitor_Back.Services;
using Supera_Monitor_Back.Services.Eventos;

namespace Supera_Monitor_Back.Controllers;

[ApiController]
[Route("back/[controller]")]
public class MonitoramentoController : _BaseController
{
	private readonly ILogService _logger;
	private readonly IMonitoramentoService _monitoramentoService;
	private readonly IEventoService _eventoService;

	public MonitoramentoController(
		IMonitoramentoService monitoramentoService,
		IEventoService eventoService,
		ILogService logger
	)
	{
		_monitoramentoService = monitoramentoService;
		_eventoService = eventoService;
		_logger = logger;
	}


	[HttpPost]
	public async Task<ActionResult<Monitoramento_Response>> GetMonitoramento(Monitoramento_Request request)
	{
		try
		{
			var response = await _monitoramentoService.GetMonitoramento(request);
			return Ok(response);
		}
		catch (Exception e)
		{
			_logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
			return StatusCode(500, e);
		}
	}

}
