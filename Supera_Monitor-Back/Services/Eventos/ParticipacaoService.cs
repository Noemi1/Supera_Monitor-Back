using System.Globalization;
using System.Text.Json;
using AutoMapper;
using AutoMapper.QueryableExtensions;
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
	public Task<ResponseModel> InsertParticipacao(InsertParticipacaoRequest request);
	public Task<ResponseModel> UpdateParticipacao(UpdateParticipacaoRequest request);
	public Task<ResponseModel> CancelarParticipacao(CancelarParticipacaoRequest request);
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

	
	public async Task<ResponseModel> InsertParticipacao(InsertParticipacaoRequest request)
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

			// Se aluno já está inscrito, não deve poder ser inscrito novamente
			bool alunoInscrito = evento.Evento_Participacao_Aluno.Any(p => p.Aluno_Id == request.Aluno_Id);
			if (alunoInscrito)
				return new ResponseModel { Message = "Aluno já está inscrito neste evento" };

			Aluno? aluno = _db.Aluno
					.Include(x => x.AulaZero)
					.ThenInclude(x => x.Evento_Participacao_Aluno)
				.FirstOrDefault(x => x.Id == request.Aluno_Id);

			if (aluno is null)
				return new ResponseModel { Message = "Aluno não encontrado" };

			var eventoList = _db.CalendarioEventoList.FirstOrDefault(x => x.Id == request.Evento_Id);
			int alunosAtivos = eventoList?.AlunosAtivosEvento ?? 0;

			if (evento.Evento_Tipo_Id == (int)EventoTipo.Aula
			 || evento.Evento_Tipo_Id == (int)EventoTipo.AulaExtra
			 || evento.Evento_Tipo_Id == (int)EventoTipo.Oficina)
			{
				if (alunosAtivos >= evento.CapacidadeMaximaAlunos)
					return new ResponseModel { Message = "Essa " + tipo +" está lotada." };
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
					item.Observacoes = $"Checklist finalizado automaticamente. <br> Aluno agendou na aula zero do dia ${evento.Data.ToString("dd/MM/yyyy HH:mm")}.";

					item.Evento_Id = evento.Id;
					_db.Aluno_Checklist_Item.Update(item);
				}
			}
			else if (evento.Evento_Tipo_Id == (int)EventoTipo.Superacao)
			{
				var item = _db.Aluno_Checklist_Item
					.FirstOrDefault(x => x.Aluno_Id == request.Aluno_Id
										&& ( x.Checklist_Item_Id == (int)ChecklistItemId.Agendamento1Superacao 
											|| x.Checklist_Item_Id == (int)ChecklistItemId.Agendamento2Superacao)
										&& x.DataFinalizacao == null);
				if (item is not null)
				{
					item.DataFinalizacao = hoje;
					item.Account_Finalizacao_Id = _account?.Id ?? 1;
					item.Observacoes = $"Checklist finalizado automaticamente. <br> Aluno agendou superação do dia ${evento.Data.ToString("dd/MM/yyyy HH:mm")}.";

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
					item.Observacoes = $"Checklist finalizado automaticamente. <br> Aluno se inscreveu na oficina do dia ${evento.Data.ToString("dd/MM/yyyy HH:mm")}.";

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

			_db.Evento_Participacao_Aluno.Add(new Evento_Participacao_Aluno()
			{
				Evento_Id = evento.Id,
				Aluno_Id = aluno.Id,
				Apostila_Abaco_Id = aluno.Apostila_Abaco_Id,
				NumeroPaginaAbaco = aluno.NumeroPaginaAbaco,
				Apostila_AH_Id = aluno.Apostila_AH_Id,
				NumeroPaginaAH = aluno.NumeroPaginaAH,
			});
			
			_db.SaveChanges();

			response.Message = $"Aluno foi inscrito no evento com sucesso";
			response.Object = await _eventoService.GetEventoById(request.Evento_Id);
			response.Success = true;
		}
		catch (Exception ex)
		{
			response.Message = $"Falha ao inscrever aluno no evento: {ex}";
		}

		return response;
	}

	public async Task<ResponseModel> UpdateParticipacao(UpdateParticipacaoRequest request)
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
			response.Object = await _eventoService.GetEventoById(participacao.Evento_Id);
		}
		catch (Exception ex)
		{
			response.Message = $"Falha ao atualizar participação do aluno: {ex}";
		}

		return response;
	}

	public async Task<ResponseModel> CancelarParticipacao(CancelarParticipacaoRequest request)
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

			checklist.ForEach(x => {
					x.Evento_Id = null;
					x.Account_Finalizacao_Id = null;
					x.DataFinalizacao = null;
				});

			if (request.ReposicaoDe_Evento_Id.HasValue)
				participacao.StatusContato_Id = (int)StatusContato.REPOSICAO_DESMARCADA;

			participacao.Deactivated = TimeFunctions.HoraAtualBR();
			participacao.AlunoContactado = request.AlunoContactado;
			participacao.ContatoObservacao = request.ContatoObservacao;
			participacao.Observacao = request.Observacao;

			_db.Aluno_Checklist_Item.RemoveRange(checklist);
			_db.Evento_Participacao_Aluno.Remove(participacao);
			_db.SaveChanges();

			response.Message = "Aluno removido scom sucesso";
			response.Object = await _eventoService.GetEventoById(participacao.Evento_Id);
			response.Success = true;
		}
		catch (Exception ex)
		{
			response.Message = $"Falha ao cancelar participação do aluno no evento: {ex}";
		}

		return response;
	}

}
