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
    public async Task<ActionResult<List<EventoAulaModel>>> GetEventoById(int eventoId)
    {
        try
        {
            var response = await _eventoService.GetEventoById(eventoId);

            return Ok(response);
        }
        catch (Exception e)
        {
            _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
            return StatusCode(500, e);
        }
    }

    [HttpPost("pseudo-aula")]
    public async Task<ActionResult<CalendarioEventoList>> GetPseudoAula(PseudoEventoRequest request)
    {
        try
        {
            var response = await _eventoService.GetPseudoAula(request);
            return Ok(response);
        }
        catch (Exception e)
        {
            _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
            return StatusCode(500, e);
        }
    }

    [HttpPost("calendario")]
    public async Task<ActionResult<List<CalendarioEventoList>>> GetCalendario(CalendarioRequest request)
    {
        try
        {
            var response =await _calendarioService.GetCalendario(request);

            return Ok(response);
        }
        catch (Exception e)
        {
            _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
            return StatusCode(500, e);
        }
    }

    [HttpPost("aulas/turma")]
    public async Task<ActionResult<ResponseModel>> InsertAulaForTurma(CreateAulaTurmaRequest request)
    {
        try
        {
            var response = await _eventoService.InsertAulaForTurma(request);

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
    public async Task<ActionResult<ResponseModel>> InsertAulaExtra(CreateAulaExtraRequest request)
    {
        try
        {
            var response = await _eventoService.InsertAulaExtra(request);

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
    public async Task<ActionResult<ResponseModel>> InsertAulaZero(CreateAulaZeroRequest request)
    {
        try
        {
            var response = await _eventoService.InsertAulaZero(request);

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
	public async Task<ActionResult<ResponseModel>> InsertOficina(CreateEventoRequest request)
	{
		try
		{
			var response = await _eventoService.Insert(request, (int)EventoTipo.Oficina);
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
	public async Task<ActionResult<ResponseModel>> InsertReuniao(CreateEventoRequest request)

	{
		try
		{
			var response = await _eventoService.Insert(request, (int)EventoTipo.Reuniao);

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
	public async Task<ActionResult<ResponseModel>> InsertSuperacao(CreateEventoRequest request)
	{
		try
		{
			var response = await _eventoService.Insert(request, (int)EventoTipo.Superacao);

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
    public async Task<ActionResult<ResponseModel>> Update(UpdateEventoRequest request)
    {
        try
        {
            var response = await _eventoService.Update(request);

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
    public async Task<ActionResult<ResponseModel>> UpdateOficina(UpdateEventoRequest request)
    {
        try
        {
            var response = await _eventoService.Update(request);

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
    public async Task<ActionResult<ResponseModel>> UpdateReuniao(UpdateEventoRequest request)
    {
        try
        {
            var response = await _eventoService.Update(request);

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
    public async Task<ActionResult<ResponseModel>> UpdateSuperacao(UpdateEventoRequest request)
    {
        try
        {
            var response = await _eventoService.Update(request);

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
    public async Task<ActionResult<ResponseModel>> Cancelar(CancelarEventoRequest request)
    {
        try
        {
            ResponseModel response = await _eventoService.Cancelar(request);

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
    public async Task<ActionResult<ResponseModel>> Finalizar(FinalizarEventoRequest request)
    {
        try
        {
            ResponseModel response = await _eventoService.Finalizar(request);

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
	public async Task<ActionResult<ResponseModel>> FinalizarAulaZero(FinalizarAulaZeroRequest request)
	{
		try
		{
			ResponseModel response = await _eventoService.FinalizarAulaZero(request);

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
	public async Task<ActionResult<ResponseModel>> Reposicao(ReposicaoRequest model)
	{
		try
		{
			ResponseModel response = await _eventoService.AgendarReposicao(model);

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
	public async Task<ActionResult<ResponseModel>> PrimeiraAula(PrimeiraAulaRequest model)
	{
		try
		{
			ResponseModel response = await _eventoService.AgendarPrimeiraAula(model);

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
	public async Task<ActionResult<ResponseModel>> InsertParticipacao(InsertParticipacaoRequest request)
	{
		try
		{
			var response = await _participacaoService.InsertParticipacao(request);

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
	public async Task<ActionResult<ResponseModel>> UpdateParticipacao(UpdateParticipacaoRequest request)
	{
		try
		{
			var response = await _participacaoService.UpdateParticipacao(request);

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
	public async Task<ActionResult<ResponseModel>> CancelarParticipacao(CancelarParticipacaoRequest request)
	{
		try
		{
			var response = await _participacaoService.CancelarParticipacao(request);

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
