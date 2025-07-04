﻿using System.Reflection;
using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Eventos;
using Supera_Monitor_Back.Models.Eventos.Aula;
using Supera_Monitor_Back.Models.Eventos.Dtos;
using Supera_Monitor_Back.Services;
using Supera_Monitor_Back.Services.Eventos;

namespace Supera_Monitor_Back.Controllers;

[Authorize(Entities.Role.Admin, Entities.Role.Teacher, Entities.Role.Assistant)]
[ApiController]
[Route("back/[controller]")]
public class EventosController : _BaseController {
    private readonly IEventoService _eventoService;
    private readonly IAulaService _aulaService;
    private readonly ILogService _logger;

    public EventosController(IEventoService eventoService, IAulaService aulaService, ILogService logger) {
        _eventoService = eventoService;
        _aulaService = aulaService;
        _logger = logger;
    }

    [HttpPost("criar")]
    public ActionResult<ResponseModel> CreateEvent(CreateEventDto dto) {
        try {
            var response = _eventoService.CreateEvent(dto);

            if (response.Success == false) {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception e) {
            _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
            return StatusCode(500, e);
        }
    }

    [HttpGet("{eventoId}")]
    public ActionResult<List<EventoAulaModel>> GetEventoById(int eventoId) {
        try {
            var response = _eventoService.GetEventoById(eventoId);

            return Ok(response);
        }
        catch (Exception e) {
            _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
            return StatusCode(500, e);
        }
    }

    [HttpGet("aulas/all")]
    public ActionResult<List<EventoAulaModel>> GetAll() {
        try {
            var response = _aulaService.GetAll();

            return Ok(response);
        }
        catch (Exception e) {
            _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
            return StatusCode(500, e);
        }
    }

    [HttpPost("calendario")]
    public ActionResult<List<CalendarioEventoList>> GetCalendario(CalendarioRequest request) {
        try {
            var response =  _eventoService.GetCalendario(request);

            return Ok(response);
        }
        catch (Exception e) {
            _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
            return StatusCode(500, e);
        }
    }

    [HttpGet("aulas/{aulaId}")]
    public ActionResult<CalendarioEventoList> GetById(int aulaId) {
        try {
            var response = _aulaService.GetById(aulaId);

            if (response is null) {
                return NotFound("Aula não encontrada");
            }

            return Ok(response);
        }
        catch (Exception e) {
            _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
            return StatusCode(500, e);
        }
    }

    [HttpPost("aulas/turma")]
    public ActionResult<ResponseModel> InsertAulaForTurma(CreateAulaTurmaRequest request) {
        try {
            var response = _aulaService.InsertAulaForTurma(request);

            if (response.Success == false) {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception e) {
            _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
            return StatusCode(500, e);
        }
    }

    [HttpPost("aulas/extra")]
    public ActionResult<ResponseModel> InsertAulaExtra(CreateAulaExtraRequest request) {
        try {
            var response = _aulaService.InsertAulaExtra(request);

            if (response.Success == false) {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception e) {
            _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
            return StatusCode(500, e);
        }
    }

    [HttpPost("aulas/zero")]
    public ActionResult<ResponseModel> InsertAulaZero(CreateAulaZeroRequest request) {
        try {
            var response = _aulaService.InsertAulaZero(request);

            if (response.Success == false) {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception e) {
            _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
            return StatusCode(500, e);
        }
    }

    [HttpPut("aulas")]
    public ActionResult<ResponseModel> Update(UpdateAulaRequest request) {
        try {
            var response = _aulaService.Update(request);

            if (response.Success == false) {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception e) {
            _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
            return StatusCode(500, e);
        }
    }

    [HttpPost("oficinas")]
    public ActionResult<ResponseModel> InsertOficina(CreateEventoRequest request) {
        try {
            var response = _eventoService.Insert(request, (int)EventoTipo.Oficina);

            if (response.Success == false) {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception e) {
            _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
            return StatusCode(500, e);
        }
    }

    [HttpPut("oficinas")]
    public ActionResult<ResponseModel> UpdateOficina(UpdateEventoRequest request) {
        try {
            var response = _eventoService.Update(request);

            if (response.Success == false) {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception e) {
            _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
            return StatusCode(500, e);
        }
    }

    [HttpPost("reunioes")]
    public ActionResult<ResponseModel> InsertReuniao(CreateEventoRequest request) {
        try {
            var response = _eventoService.Insert(request, (int)EventoTipo.Reuniao);

            if (response.Success == false) {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception e) {
            _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
            return StatusCode(500, e);
        }
    }

    [HttpPut("reunioes")]
    public ActionResult<ResponseModel> UpdateReuniao(UpdateEventoRequest request) {
        try {
            var response = _eventoService.Update(request);

            if (response.Success == false) {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception e) {
            _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
            return StatusCode(500, e);
        }
    }

    [HttpPost("superacao")]
    public ActionResult<ResponseModel> InsertSuperacao(CreateEventoRequest request) {
        try {
            var response = _eventoService.Insert(request, (int)EventoTipo.Superacao);

            if (response.Success == false) {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception e) {
            _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
            return StatusCode(500, e);
        }
    }

    [HttpPut("superacao")]
    public ActionResult<ResponseModel> UpdateSuperacao(UpdateEventoRequest request) {
        try {
            var response = _eventoService.Update(request);

            if (response.Success == false) {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception e) {
            _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
            return StatusCode(500, e);
        }
    }

    [HttpPost("inscrever")]
    public ActionResult<ResponseModel> InsertParticipacao(InsertParticipacaoRequest request) {
        try {
            var response = _eventoService.InsertParticipacao(request);

            if (response.Success == false) {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception e) {
            _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
            return StatusCode(500, e);
        }
    }

    [HttpDelete("remover")]
    public ActionResult<ResponseModel> RemoveParticipacao(int participacaoId) {
        try {
            var response = _eventoService.RemoveParticipacao(participacaoId);

            if (response.Success == false) {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception e) {
            _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
            return StatusCode(500, e);
        }
    }

    [HttpPost("aulas/chamada")]
    public ActionResult<ResponseModel> Chamada(ChamadaRequest request) {
        try {
            ResponseModel response = _aulaService.Chamada(request);

            if (response.Success) {
                return Ok(response);
            }

            return BadRequest(response);
        }
        catch (Exception e) {
            _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
            return StatusCode(500, e);
        }
    }

    [HttpPost("cancelar")]
    public ActionResult<ResponseModel> Cancelar(CancelarEventoRequest request) {
        try {
            ResponseModel response = _eventoService.Cancelar(request);

            if (response.Success) {
                return Ok(response);
            }

            return BadRequest(response);
        }
        catch (Exception e) {
            _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
            return StatusCode(500, e);
        }
    }

    [HttpGet("aulas/alunos/{ano}")]
    public ActionResult<List<Evento_Aula_Aluno>> AlunosAulas(int ano) {
        try {
            // Se ano for menor que 2025, ele será ajustado para 2025
            // Se ano for maior que o ano atual, ele será ajustado para o ano atual
            // Se ano já estiver dentro do intervalo, ele permanece inalterado.
            ano = Math.Clamp(ano, 2025, DateTime.Now.Year);

            var response = _aulaService.AlunosAulas(ano);

            return Ok(response);
        }
        catch (Exception e) {
            _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
            return StatusCode(500, e);
        }
    }

    [HttpPost("reagendar")]
    public ActionResult<ResponseModel> Reagendar(ReagendarEventoRequest request) {
        try {
            ResponseModel response = _eventoService.Reagendar(request);

            if (response.Success) {
                return Ok(response);
            }

            return BadRequest(response);
        }
        catch (Exception e) {
            _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
            return StatusCode(500, e);
        }
    }

    [HttpPost("finalizar")]
    public ActionResult<ResponseModel> Finalizar(FinalizarEventoRequest request) {
        try {
            ResponseModel response = _eventoService.Finalizar(request);

            if (response.Success) {
                return Ok(response);
            }

            return BadRequest(response);
        }
        catch (Exception e) {
            _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
            return StatusCode(500, e);
        }
    }

    [HttpGet("oficinas/all")]
    public ActionResult<ResponseModel> GetOficinas() {
        try {
            var response = _eventoService.GetOficinas();

            return Ok(response);
        }
        catch (Exception e) {
            _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
            return StatusCode(500, e);
        }
    }

    [HttpPost("dashboard")]
    public ActionResult<Dashboard> Dashboard(DashboardRequest request) {
        try {
            // Se ano for menor que 2025, ele será ajustado para 2025
            // Se ano for maior que o ano atual, ele será ajustado para o ano atual
            // Se ano já estiver dentro do intervalo, ele permanece inalterado.
            request.Ano = Math.Clamp(request.Ano, 2025, DateTime.Now.Year);
            //request.Mes = Math.Clamp(request.Mes, 0, 12);

            var response = _eventoService.Dashboard(request);

            return Ok(response);
        }
        catch (Exception e) {
            _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
            return StatusCode(500, e);
        }
    }

    [HttpPost("cancelar-eventos-feriado/{ano}")]
    public async Task<ActionResult<ResponseModel>> CancelaEventos(int ano)
    {
        try
        {
            ano = Math.Clamp(ano, 2000, DateTime.Now.Year + 100);
            var response = await _eventoService.CancelaEventosFeriado(ano);
            return Ok(response);
        }
        catch (Exception e)
        {
            _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
            return StatusCode(500, e);
        }
    }

}
