using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Aluno;
using Supera_Monitor_Back.Models.Eventos;
using Supera_Monitor_Back.Models.Eventos.Aula;
using Supera_Monitor_Back.Models.Eventos.Participacao;
using Supera_Monitor_Back.Services;
using Supera_Monitor_Back.Services.Eventos;

namespace Supera_Monitor_Back.Controllers;

//[Authorize(Entities.Role.Admin, Entities.Role.Teacher, Entities.Role.Assistant)]
[ApiController]
[Route("back/[controller]")]
public class EventosController : _BaseController
{
    private readonly IEventoService _eventoService;
    private readonly IParticipacaoService _participacaoService;
    private readonly ILogService _logger;
    private readonly ICalendarioService _calendarioService;

	public EventosController(
		IEventoService eventoService, 
		IParticipacaoService participacaoService, 
		ICalendarioService calendarioService,
		ILogService logger
	)
    {
        _eventoService = eventoService;
		_participacaoService = participacaoService;
        _calendarioService = calendarioService;
        _logger = logger;
    }

	[HttpGet("{eventoId}")]
	public ActionResult<List<EventoAulaModel>> GetEventoById(int eventoId)
	{
		try
		{
			var response = _eventoService.GetEventoById(eventoId);

			return Ok(response);
		}
		catch (Exception e)
		{
			_logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
			return StatusCode(500, e);
		}
	}
	
	//[HttpGet("feriados/{ano}")]
	//public async Task<ActionResult<List<FeriadoResponse>>> GetFeriados(int ano)
	//{
	//	try
	//	{
	//		var response =  _eventoService.GetFeriados(ano);

	//		return Ok(response);
	//	}
	//	catch (Exception e)
	//	{
	//		_logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
	//		return StatusCode(500, e);
	//	}
	//}

	[HttpPost("pseudo-aula")]
    public ActionResult<CalendarioEventoList> GetPseudoAula(PseudoEventoRequest request)
    {
        try
        {
            var response = _eventoService.GetPseudoAula(request);
            return Ok(response);
        }
        catch (Exception e)
        {
            _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
            return StatusCode(500, e);
        }
    }

    [HttpPost("calendario")]
    public ActionResult<CalendarioResponse> GetCalendario(CalendarioRequest request)
    {
        try
        {
            var response = _calendarioService.GetCalendario(request);

            return Ok(response);
        }
        catch (Exception e)
        {
            _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
            return StatusCode(500, e);
        }
    }

    [HttpPost("aulas/turma")]
    public ActionResult<ResponseModel> InsertAulaForTurma(CreateAulaTurmaRequest request)
    {
        try
        {
            var response = _eventoService.InsertAulaForTurma(request);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception e)
        {
            _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
            return StatusCode(500, e);
        }
    }

    [HttpPost("aulas/extra")]
    public ActionResult<ResponseModel> InsertAulaExtra(CreateAulaExtraRequest request)
    {
        try
        {
            var response =  _eventoService.InsertAulaExtra(request);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception e)
        {
            _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
            return StatusCode(500, e);
        }
    }

    [HttpPost("aulas/zero")]
    public ActionResult<ResponseModel> InsertAulaZero(CreateAulaZeroRequest request)
    {
        try
        {
            var response =  _eventoService.InsertAulaZero(request);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception e)
        {
            _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
            return StatusCode(500, e);
        }
    }

	[HttpPost("oficinas")]
	public ActionResult<ResponseModel> InsertOficina(CreateEventoRequest request)
	{
		try
		{
			var response =  _eventoService.Insert(request, (int)EventoTipo.Oficina);
			if (!response.Success)
			{
				return BadRequest(response);
			}

			return Ok(response);
		}
		catch (Exception e)
		{
			_logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
			return StatusCode(500, e);
		}
	}

	[HttpPost("reunioes")]
	public ActionResult<ResponseModel> InsertReuniao(CreateEventoRequest request)

	{
		try
		{
			var response =  _eventoService.Insert(request, (int)EventoTipo.Reuniao);

			if (!response.Success)
			{
				return BadRequest(response);
			}

			return Ok(response);
		}
		catch (Exception e)
		{
			_logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
			return StatusCode(500, e);
		}
	}
	
	[HttpPost("superacao")]
	public ActionResult<ResponseModel> InsertSuperacao(CreateEventoRequest request)
	{
		try
		{
			var response =  _eventoService.Insert(request, (int)EventoTipo.Superacao);

			if (!response.Success)
			{
				return BadRequest(response);
			}

			return Ok(response);
		}
		catch (Exception e)
		{
			_logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
			return StatusCode(500, e);
		}
	}

	[HttpPut("aulas")]
    public ActionResult<ResponseModel> Update(UpdateEventoRequest request)
    {
        try
        {
            var response =  _eventoService.Update(request);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception e)
        {
            _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
            return StatusCode(500, e);
        }
    }

    [HttpPut("oficinas")]
    public ActionResult<ResponseModel> UpdateOficina(UpdateEventoRequest request)
    {
        try
        {
            var response =  _eventoService.Update(request);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception e)
        {
            _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
            return StatusCode(500, e);
        }
    }

    [HttpPut("reunioes")]
    public ActionResult<ResponseModel> UpdateReuniao(UpdateEventoRequest request)
    {
        try
        {
            var response =  _eventoService.Update(request);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception e)
        {
            _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
            return StatusCode(500, e);
        }
    }
    
    [HttpPut("superacao")]
    public ActionResult<ResponseModel> UpdateSuperacao(UpdateEventoRequest request)
    {
        try
        {
            var response =  _eventoService.Update(request);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception e)
        {
            _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
            return StatusCode(500, e);
        }
    }

    [HttpPost("cancelar")]
    public ActionResult<ResponseModel> Cancelar(CancelarEventoRequest request)
    {
        try
        {
            ResponseModel response =  _eventoService.Cancelar(request);

            if (response.Success)
            {
                return Ok(response);
            }

            return BadRequest(response);
        }
        catch (Exception e)
        {
            _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
            return StatusCode(500, e);
        }
    }

    [HttpPost("finalizar")]
    public ActionResult<ResponseModel> Finalizar(FinalizarEventoRequest request)
    {
        try
        {
            ResponseModel response =  _eventoService.Finalizar(request);

            if (response.Success)
            {
                return Ok(response);
            }

            return BadRequest(response);
        }
        catch (Exception e)
        {
            _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
            return StatusCode(500, e);
        }
    }

	[HttpPost("aula-zero/finalizar")]
	public ActionResult<ResponseModel> FinalizarAulaZero(FinalizarAulaZeroRequest request)
	{
		try
		{
			ResponseModel response =  _eventoService.FinalizarAulaZero(request);

			if (response.Success)
			{
				return Ok(response);
			}

			return BadRequest(response);
		}
		catch (Exception e)
		{
			_logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
			return StatusCode(500, e);
		}
	}

	[HttpPost("reposicao")]
	public ActionResult<ResponseModel> Reposicao(ReposicaoRequest model)
	{
		try
		{
			ResponseModel response =  _eventoService.AgendarReposicao(model);

			if (response.Success)
			{
				// _logger.Log("Reposicao", "Aluno", response, Account?.Id);
				return Ok(response);
			}

			return BadRequest(response);
		}
		catch (Exception e)
		{
			// _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
			return StatusCode(500, e);
		}
	}

	[HttpPost("primeira-aula")]
	public ActionResult<ResponseModel> PrimeiraAula(PrimeiraAulaRequest model)
	{
		try
		{
			ResponseModel response =  _eventoService.AgendarPrimeiraAula(model);

			if (response.Success)
			{
				// _logger.Log("Primeira Aula", "Aluno", response, Account?.Id);
				return Ok(response);
			}

			return BadRequest(response);
		}
		catch (Exception e)
		{
			// _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
			return StatusCode(500, e);
		}
	}
	

	//
	// Participacao
	//
	[HttpPost("participacao/inscrever")]
	public ActionResult<ResponseModel> InsertParticipacao(InsertParticipacaoRequest request)
	{
		try
		{
			var response =  _participacaoService.InsertParticipacao(request);

			if (!response.Success)
				return BadRequest(response);

			return Ok(response);
		}
		catch (Exception e)
		{
			_logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
			return StatusCode(500, e);
		}
	}

	[HttpPut("participacao/atualizar")]
	public ActionResult<ResponseModel> UpdateParticipacao(UpdateParticipacaoRequest request)
	{
		try
		{
			var response =  _participacaoService.UpdateParticipacao(request);

			if (!response.Success)
				return BadRequest(response);

			return Ok(response);
		}
		catch (Exception e)
		{
			_logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
			return StatusCode(500, e);
		}
	}

	[HttpPatch("participacao/cancelar")]
	public ActionResult<ResponseModel> CancelarParticipacao(CancelarParticipacaoRequest request)
	{
		try
		{
			var response =  _participacaoService.CancelarParticipacao(request);

			if (!response.Success)
				return BadRequest(response);

			return Ok(response);
		}
		catch (Exception e)
		{
			_logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
			return StatusCode(500, e);
		}
	}
	[HttpPatch("participacao/cancelar-falta/{participacaoId}")]
	public ActionResult<ResponseModel> CancelarFaltaAgendada(int participacaoId)
	{
		try
		{
			var response =  _participacaoService.CancelarFaltaAgendada(participacaoId);

			if (!response.Success)
				return BadRequest(response);

			return Ok(response);
		}
		catch (Exception e)
		{
			_logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
			return StatusCode(500, e);
		}
	}

}
