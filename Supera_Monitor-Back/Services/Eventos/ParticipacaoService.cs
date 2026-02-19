using System.Globalization;
using System.Text.Json;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Azure.Core;
using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Aluno;
using Supera_Monitor_Back.Models.Eventos;
using Supera_Monitor_Back.Models.Eventos.Dtos;
using Supera_Monitor_Back.Models.Eventos.Participacao;

namespace Supera_Monitor_Back.Services.Eventos;

public interface IParticipacaoService
{
    ResponseModel InsertParticipacao(InsertParticipacaoRequest request);
    ResponseModel UpdateParticipacao(UpdateParticipacaoRequest request);
    ResponseModel CancelarParticipacao(CancelarParticipacaoRequest request);

    ResponseModel CancelarFaltaAgendada(int participacaoId);
}

public class ParticipacaoService : IParticipacaoService
{
    private readonly DataContext _db;
    private readonly IMapper _mapper;
    private readonly IEventoService _eventoService;

    private readonly Account? _account;

    public ParticipacaoService(
        DataContext db,
        IMapper mapper,
        IEventoService eventoService,
        IHttpContextAccessor httpContextAccessor
    )
    {
        _db = db;
        _mapper = mapper;
        _eventoService = eventoService;
        _account = (Account?)httpContextAccessor.HttpContext?.Items["Account"];
    }


    public ResponseModel InsertParticipacao(InsertParticipacaoRequest request)
    {
        ResponseModel response = new() { Success = false };

        try
        {
            Evento? evento = _db.Evento
                .Include(e => e.Evento_Aula)
                .Include(e => e.Evento_Participacao_Aluno)
                .ThenInclude(x => x.Aluno)
                .Include(e => e.Evento_Tipo)
                .FirstOrDefault(e => e.Id == request.Evento_Id);

            if (evento == null)
                return new ResponseModel { Message = "Evento não encontrado" };

            var tipo = evento.Evento_Tipo.Nome ?? "aula";

            ResponseModel eventValidation = EventoUtils.ValidateEvent(evento);

            if (!eventValidation.Success)
                return eventValidation;

            Aluno? aluno = _db.Aluno
                    .Include(x => x.AulaZero)
                    .ThenInclude(x => x.Evento_Participacao_Aluno)
                .FirstOrDefault(x => x.Id == request.Aluno_Id);

            if (aluno is null)
                return new ResponseModel { Message = "Aluno não encontrado" };

            var eventoList = _db.CalendarioEventoList.FirstOrDefault(x => x.Id == request.Evento_Id);
            int alunosAtivos = eventoList?.AlunosAtivosEvento ?? 0;

            if (evento.Evento_Tipo_Id == (int)EventoTipo.Aula
             || evento.Evento_Tipo_Id == (int)EventoTipo.TurmaExtra
             || evento.Evento_Tipo_Id == (int)EventoTipo.Oficina)
            {
                if (eventoList.VagasDisponiveisEvento < 1)
                    return new ResponseModel { Message = "Essa " + tipo + " está lotada." };
            }

            var hoje = TimeFunctions.HoraAtualBR();

            //
            // Atualiza checklist
            //
            if (evento.Evento_Tipo_Id == (int)EventoTipo.AulaZero)
            {
                if (aluno.AulaZero is not null)
                {
                    var aulaZero = aluno.AulaZero;
                    var participacaoAulaZero = aulaZero.Evento_Participacao_Aluno
                        .FirstOrDefault(x => x.Aluno_Id == request.Aluno_Id);

                    if (participacaoAulaZero?.Presente == true)
                        return new ResponseModel { Message = $"Aluno já participou da aula zero no dia: {aulaZero.Data.ToString("dd/MM/yyyy HH:mm")}" };
                    else
                    {
                        aluno.AulaZero_Id = evento.Id;
                        _db.Aluno.Update(aluno);
                    }
                }
                var item = _db.Aluno_Checklist_Item
                    .FirstOrDefault(x => x.Aluno_Id == request.Aluno_Id
                                        && x.Checklist_Item_Id == (int)ChecklistItemId.AgendamentoAulaZero
                                        && x.DataFinalizacao == null);

                if (item is not null)
                {
                    item.DataFinalizacao = hoje;
                    item.Account_Finalizacao_Id = _account?.Id ?? 1;
                    item.Observacoes = $"Checklist finalizado automaticamente. <br> Aluno agendou na aula zero do dia {evento.Data.ToString("dd/MM/yyyy \'às\' HH:mm")}.";

                    item.Evento_Id = evento.Id;
                    _db.Aluno_Checklist_Item.Update(item);
                }
            }
            else if (evento.Evento_Tipo_Id == (int)EventoTipo.Superacao)
            {
                var item = _db.Aluno_Checklist_Item
                    .FirstOrDefault(x => x.Aluno_Id == request.Aluno_Id
                                        && (x.Checklist_Item_Id == (int)ChecklistItemId.Agendamento1Superacao
                                            || x.Checklist_Item_Id == (int)ChecklistItemId.Agendamento2Superacao)
                                        && x.DataFinalizacao == null);
                if (item is not null)
                {
                    item.DataFinalizacao = hoje;
                    item.Account_Finalizacao_Id = _account?.Id ?? 1;
                    item.Observacoes = $"Checklist finalizado automaticamente. <br> Aluno agendou superação do dia {evento.Data.ToString("dd/MM/yyyy \'às\' HH:mm")}.";

                    item.Evento_Id = evento.Id;
                    _db.Aluno_Checklist_Item.Update(item);
                }
            }
            else if (evento.Evento_Tipo_Id == (int)EventoTipo.Oficina)
            {
                var item = _db.Aluno_Checklist_Item
                    .FirstOrDefault(x => x.Aluno_Id == request.Aluno_Id
                                        && (x.Checklist_Item_Id == (int)ChecklistItemId.Agendamento1Oficina
                                            || x.Checklist_Item_Id == (int)ChecklistItemId.Agendamento2Oficina)
                                        && x.DataFinalizacao == null);
                if (item is not null)
                {
                    item.DataFinalizacao = hoje;
                    item.Account_Finalizacao_Id = _account?.Id ?? 1;
                    item.Observacoes = $"Checklist finalizado automaticamente. <br> Aluno se inscreveu na oficina do dia {evento.Data.ToString("dd/MM/yyyy \'às\' HH:mm")}.";

                    item.Evento_Id = evento.Id;
                    _db.Aluno_Checklist_Item.Update(item);
                }
            }


            //
            // Validations passed
            //

            _db.Aluno_Historico.Add(new Aluno_Historico
            {
                Aluno_Id = aluno.Id,
                Descricao = $"Aluno foi inscrito no evento '{evento.Descricao}' do dia {evento.Data:G} - Evento é do tipo '{evento.Evento_Tipo.Nome}'",
                Account_Id = _account!.Id,
                Data = hoje,
            });

            var alunoInscrito = evento.Evento_Participacao_Aluno.FirstOrDefault(p => p.Aluno_Id == request.Aluno_Id);

            if (alunoInscrito is not null)
            {
                alunoInscrito.Presente = null;
                alunoInscrito.Deactivated = null;
                alunoInscrito.AgendouFalta = null;
                alunoInscrito.Apostila_Abaco_Id = aluno.Apostila_Abaco_Id;
                alunoInscrito.Apostila_AH_Id = aluno.Apostila_AH_Id;
                alunoInscrito.NumeroPaginaAbaco = aluno.NumeroPaginaAbaco;
                alunoInscrito.NumeroPaginaAH = aluno.NumeroPaginaAH;

                _db.Evento_Participacao_Aluno.Update(alunoInscrito);
            }
            else
            {

                _db.Evento_Participacao_Aluno.Add(new Evento_Participacao_Aluno()
                {
                    Evento_Id = evento.Id,
                    Aluno_Id = aluno.Id,
                    Apostila_Abaco_Id = aluno.Apostila_Abaco_Id,
                    NumeroPaginaAbaco = aluno.NumeroPaginaAbaco,
                    Apostila_AH_Id = aluno.Apostila_AH_Id,
                    NumeroPaginaAH = aluno.NumeroPaginaAH,
                });

            }


            _db.SaveChanges();

            response.Message = $"Aluno foi inscrito no evento com sucesso";
            response.Object = _eventoService.GetEventoById(request.Evento_Id);
            response.Success = true;
        }
        catch (Exception ex)
        {
            response.Message = $"Falha ao inscrever aluno no evento: {ex}";
        }

        return response;
    }

    public ResponseModel UpdateParticipacao(UpdateParticipacaoRequest request)
    {
        ResponseModel response = new() { Success = false };
        try
        {
            var participacao = _db.Evento_Participacao_Aluno.Find(request.Participacao_Id);
            if (participacao is null)
                return new ResponseModel { Message = "Participação não encontrada" };

            Apostila? apostilaAbaco = _db.Apostila.Find(request.Apostila_Abaco_Id);

            if (request.Apostila_Abaco_Id.HasValue && apostilaAbaco is null)
                return new ResponseModel { Message = "Apostila Ábaco não encontrada" };

            Apostila? apostilaAh = _db.Apostila.Find(request.Apostila_AH_Id);
            if (request.Apostila_Abaco_Id.HasValue && apostilaAbaco is null)
                return new ResponseModel { Message = "Apostila AH não encontrada" };

            // Validations passed

            participacao.Observacao = request.Observacao;
            participacao.Deactivated = request.Deactivated;

            participacao.Apostila_Abaco_Id = request.Apostila_Abaco_Id;
            participacao.NumeroPaginaAbaco = request.NumeroPaginaAbaco;
            participacao.Apostila_AH_Id = request.Apostila_AH_Id;
            participacao.NumeroPaginaAH = request.NumeroPaginaAH;

            participacao.AlunoContactado = request.AlunoContactado;
            participacao.ContatoObservacao = request.ContatoObservacao;
            participacao.StatusContato_Id = request.StatusContato_Id;

            _db.Evento_Participacao_Aluno.Update(participacao);
            _db.SaveChanges();

            response.Message = "Participação do aluno foi atualizada com sucesso.";
            response.Success = true;
            response.Object = _eventoService.GetEventoById(participacao.Evento_Id);
        }
        catch (Exception ex)
        {
            response.Message = $"Falha ao atualizar participação do aluno: {ex}";
        }

        return response;
    }

    public ResponseModel CancelarParticipacao(CancelarParticipacaoRequest request)
    {
        ResponseModel response = new() { Success = false };

        try
        {
            var participacao = _db.Evento_Participacao_Aluno
                .Include(e => e.Aluno)
                .FirstOrDefault(p => p.Id == request.Participacao_Id);

            var evento = _db.Evento
                .Include(x => x.Evento_Tipo)
                .FirstOrDefault(x => x.Id == participacao.Evento_Id);

            if (evento is null)
                return new ResponseModel { Message = "Evento não encontrado." };

            if (evento.Deactivated.HasValue)
                return new ResponseModel { Message = "Evento inativo." };

            if (evento.Finalizado)
                return new ResponseModel { Message = "Evento já foi finalizado." };

            if (participacao is null)
                return new ResponseModel { Message = "Aluno não encontrado." };

            if (participacao.Presente == true)
                return new ResponseModel { Message = "Aluno já participou dessa " + evento.Evento_Tipo.Nome + "." };

            if (participacao.Deactivated.HasValue)
                return new ResponseModel { Message = "O aluno não participa mais dessa " + evento.Evento_Tipo.Nome + "." };

            //
            // Validations passed
            //

            if (participacao.Aluno.PrimeiraAula_Id == participacao.Evento_Id)
                participacao.Aluno.PrimeiraAula_Id = null;

            if (participacao.Aluno.AulaZero_Id == participacao.Evento_Id)
                participacao.Aluno.AulaZero_Id = null;

            var checklist = _db.Aluno_Checklist_Item
                .Where(x => x.Evento_Id == evento.Id)
                .ToList();

            checklist.ForEach(x =>
            {
                x.Evento_Id = null;
                x.Account_Finalizacao_Id = null;
                x.DataFinalizacao = null;
            });

            if (request.ReposicaoDe_Evento_Id.HasValue)
                participacao.StatusContato_Id = (int)StatusContato.REPOSICAO_DESMARCADA;


            participacao.Presente = false;
            participacao.Deactivated = TimeFunctions.HoraAtualBR();
            participacao.AlunoContactado = request.AlunoContactado;
            participacao.ContatoObservacao = request.ContatoObservacao;
            participacao.Observacao = request.Observacao;
            participacao.AgendouFalta = participacao.Deactivated;

            _db.Aluno_Checklist_Item.UpdateRange(checklist);
            _db.Evento_Participacao_Aluno.Update(participacao);
            _db.SaveChanges();

            response.Message = "Aluno removido com sucesso";
            response.Object = _eventoService.GetEventoById(participacao.Evento_Id);
            response.Success = true;
        }
        catch (Exception ex)
        {
            response.Message = $"Falha ao cancelar participação do aluno no evento: {ex}";
        }

        return response;
    }


    public ResponseModel CancelarFaltaAgendada(int participacaoId)
    {
        ResponseModel response = new() { Success = false };

        try
        {

            var participacao = _db.Evento_Participacao_Aluno
                .Include(e => e.Aluno)
                .FirstOrDefault(p => p.Id == participacaoId);

            var evento = _db.CalendarioEventoList
                .FirstOrDefault(x => x.Id == participacao.Evento_Id);


            var tipo = EventoUtils.GetTipo(evento.Evento_Tipo_Id);

            if (participacao is null)
                return new ResponseModel { Message = "Aluno não encontrado." };

            if (participacao.Presente == true)
                return new ResponseModel { Message = $"Aluno já participou dessa {tipo}." };

            if (evento == null)
                return new ResponseModel { Message = $"Evento não encontrado." };

            if (evento.Deactivated.HasValue)
                return new ResponseModel { Message = $"{tipo} está inativa." };

            if (evento.Finalizado)
                return new ResponseModel { Message = $"{tipo} já foi finalizada." };

            if (evento.VagasDisponiveisEvento < 1)
                return new ResponseModel { Message = $"Capacidade máxima " };

            if (evento.Evento_Tipo_Id == (int)EventoTipo.Aula
             || evento.Evento_Tipo_Id == (int)EventoTipo.TurmaExtra
             || evento.Evento_Tipo_Id == (int)EventoTipo.Oficina)
            {
                if (evento.VagasDisponiveisEvento < 1)
                    return new ResponseModel { Message = $"Essa {evento.Evento_Tipo} está lotada." };
            }


            var eventoEntity = _db.Evento.Find(participacao.Evento_Id);

            if (eventoEntity is not null && evento.AlunosAtivosEvento == 1)
            {
                eventoEntity.Deactivated = null;
                eventoEntity.Observacao = null;

                _db.Evento.Update(eventoEntity);
            }


            var hoje = TimeFunctions.HoraAtualBR();

            //
            // Atualiza checklist
            //
            var aluno = _db.Aluno.Find(participacao.Aluno_Id);

            if (aluno is not null)
            {
                Aluno_Checklist_Item? item = null;
                string? observacoes = null;

                if (evento.Evento_Tipo_Id == (int)EventoTipo.AulaZero)
                {
                    aluno.AulaZero_Id = evento.Id;
                    _db.Aluno.Update(aluno);

                    item = _db.Aluno_Checklist_Item
                        .FirstOrDefault(x => x.Aluno_Id == aluno.Id
                                            && x.Checklist_Item_Id == (int)ChecklistItemId.AgendamentoAulaZero
                                            && x.DataFinalizacao == null);
                    observacoes = $"Checklist finalizado automaticamente. <br> Aluno agendou na aula zero do dia {evento.Data.ToString("dd/MM/yyyy \'às\' HH:mm")}.";

                }
                else if (evento.Evento_Tipo_Id == (int)EventoTipo.Superacao)
                {
                    observacoes = $"Checklist finalizado automaticamente. <br> Aluno agendou superação do dia {evento.Data.ToString("dd/MM/yyyy \'às\' HH:mm")}.";
                    item = _db.Aluno_Checklist_Item
                        .FirstOrDefault(x => x.Aluno_Id == aluno.Id
                                            && (x.Checklist_Item_Id == (int)ChecklistItemId.Agendamento1Superacao
                                                || x.Checklist_Item_Id == (int)ChecklistItemId.Agendamento2Superacao)
                                            && x.DataFinalizacao == null);
                }
                else if (evento.Evento_Tipo_Id == (int)EventoTipo.Oficina)
                {
                    item = _db.Aluno_Checklist_Item
                        .FirstOrDefault(x => x.Aluno_Id == aluno.Id
                                            && (x.Checklist_Item_Id == (int)ChecklistItemId.Agendamento1Oficina
                                                || x.Checklist_Item_Id == (int)ChecklistItemId.Agendamento2Oficina)
                                            && x.DataFinalizacao == null);
                    observacoes = $"Checklist finalizado automaticamente. <br> Aluno se inscreveu na oficina do dia {evento.Data.ToString("dd/MM/yyyy \'às\' HH:mm")}.";
                  
                }
                else if (participacao.PrimeiraAula == true)
                {
                    aluno.PrimeiraAula_Id = evento.Id;
                    _db.Aluno.Update(aluno);

                    item = _db.Aluno_Checklist_Item
                        .FirstOrDefault(x => x.Aluno_Id == aluno.Id
                                            && (x.Checklist_Item_Id == (int)ChecklistItemId.AgendamentoPrimeiraAula)
                                            && x.DataFinalizacao == null);
                    observacoes = $"Checklist finalizado automaticamente. <br> Aluno agendou primeira aula para o dia {evento.Data.ToString("dd/MM/yyyy \'às\' HH:mm")}.";


                }

                if (item is not null)
                {
                    item.Observacoes = observacoes;
                    item.DataFinalizacao = hoje;
                    item.Account_Finalizacao_Id = _account?.Id ?? 1;
                    item.Evento_Id = evento.Id;

                    _db.Aluno_Checklist_Item.Update(item);
                }
            }


            //
            // Salva Evento_Participacao_Aluno
            //
            participacao.Deactivated = null;
            participacao.AgendouFalta = null;
            participacao.AlunoContactado = null;
            participacao.ContatoObservacao = null;
            participacao.StatusContato_Id = null;
            _db.Evento_Participacao_Aluno.Update(participacao);

            _db.SaveChanges();

            response.Message = "Falta cancelada com sucesso";
            response.Object = _eventoService.GetEventoById(participacao.Evento_Id);
            response.Success = true;
        }

        catch (Exception ex)
        {
            response.Message = $"Falha ao cancelar participação do aluno no evento: {ex}";
        }


        return response;
    }

}
