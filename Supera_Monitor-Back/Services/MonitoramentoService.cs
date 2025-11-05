using System.Globalization;
using System.Linq;
using AutoMapper;
using Azure;
using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models.Eventos;
using Supera_Monitor_Back.Models.Monitoramento;
using Supera_Monitor_Back.Models.Roteiro;
using Supera_Monitor_Back.Services.Eventos;

namespace Supera_Monitor_Back.Services;

public interface IMonitoramentoService
{
	public Task<Monitoramento_Response> GetMonitoramento(Monitoramento_Request request);
}

public class MonitoramentoService : IMonitoramentoService
{
	private DataContext _db;
	private readonly IMapper _mapper;
	private readonly IEventoService _eventoService;
	private readonly IRoteiroService _roteiroService;

	public MonitoramentoService(
		DataContext db,
		IMapper mapper,
		IRoteiroService roteiroService,
		IEventoService eventoService,
		ISalaService salaService
	)
	{
		_db = db;
		_mapper = mapper;
		_roteiroService = roteiroService;
		_eventoService = eventoService;
	}


	public async Task<Monitoramento_Response> GetMonitoramento(Monitoramento_Request request)
	{
		DateTime intervaloDe = new(request.Ano, 1, 1);
		DateTime intervaloAte = intervaloDe.AddYears(1).AddDays(-1);

		var response = new Monitoramento_Response();

		var eventosQueryable = _db.CalendarioEventoLists
				.Where(x => x.Data.Date >= intervaloDe.Date && x.Data.Date <= intervaloAte.Date
							&& (x.Evento_Tipo_Id == (int)EventoTipo.Aula || x.Evento_Tipo_Id == (int)EventoTipo.AulaExtra));

		var participacoesQueryable = _db.CalendarioAlunoLists
			.AsQueryable();

		var turmasQueryable = _db.TurmaLists
			.Where(t => t.Deactivated == null);


		var alunosQueryable = _db.AlunoLists
			.Where(t => t.Deactivated == null);


		if (request.Turma_Id.HasValue)
		{
			alunosQueryable = alunosQueryable.Where(x => x.Turma_Id == request.Turma_Id.Value);
		}

		if (request.Aluno_Id.HasValue)
		{
			alunosQueryable = alunosQueryable.Where(x => x.Id == request.Aluno_Id.Value);
			participacoesQueryable = participacoesQueryable.Where(x => x.Aluno_Id == request.Aluno_Id.Value);
		}

		if (request.Professor_Id.HasValue)
		{
			alunosQueryable = alunosQueryable.Where(x => x.Professor_Id == request.Professor_Id.Value);
		}

		var alunosIds = alunosQueryable.AsNoTracking().Select(x => x.Id);
		var turmasIds = turmasQueryable.AsNoTracking().Select(x => x.Id);

		var alunos = alunosQueryable.AsNoTracking().ToList();
		var turmas = turmasQueryable.AsNoTracking().ToList();
		var participacoes = participacoesQueryable.AsNoTracking().ToList();
		var eventos = eventosQueryable.AsNoTracking().ToList();
		var vigencias = _db.Aluno_Turma_Vigencia
			.Where(x => alunosIds.Contains(x.Aluno_Id)
						&& turmasIds.Contains(x.Turma_Id)
						&& x.DataInicioVigencia.Date >= intervaloDe
						&& (!x.DataFimVigencia.HasValue || x.DataFimVigencia.Value.Date <= intervaloAte))
			.AsNoTracking()
			.ToList();

		//
		// Somente alunos com alguma vigencia poderão participar do monitoramento (redução de request)
		//
		var alunosVigenciasIds = vigencias.Select(x => x.Aluno_Id);
		alunos = alunos.Where(x => alunosVigenciasIds.Contains(x.Id)).ToList();

		var roteirosTask = _roteiroService.GetAllAsync(request.Ano);
		var feriadosTask = _eventoService.GetFeriados(request.Ano);

		await Task.WhenAll(roteirosTask, feriadosTask);

		var roteiros = roteirosTask.Result;
		var feriados = feriadosTask.Result;

		var eventosPorId = eventos
			.ToDictionary(e => e.Id, e => e);

		var eventosPorTurmaData = eventos
			.GroupBy(x => (x.Turma_Id, x.Data.Date))
			.ToDictionary(g => g.Key, g => g.First());

		var eventosPorTurmaAluno = eventos
			.GroupBy(x => (x.Turma_Id, x.Data.Date))
			.ToDictionary(g => g.Key, g => g.First());

		//var participacoesPorEvento = participacoes
		//	.GroupBy(x => x.Evento_Id)
		//	.ToDictionary(g => g.Key, g => g.ToList());

		var participacaoPorAlunoEvento = participacoes
			.GroupBy(x => new { x.Aluno_Id, x.Evento_Id })
			.ToDictionary(g => (g.Key.Aluno_Id, g.Key.Evento_Id), g => g.First());

		string dateFormatDict = "ddMMyyyyHHmm";

		var feriadosPorData = feriados
			.ToDictionary(x => x.date.ToString(dateFormatDict), x => x);

		var roteirosPorId = roteiros.Where(x => x.Id != -1)
			.ToDictionary(r => r.Id, r => r);

		var turmasPorId = turmas
			.ToDictionary(x => x.Id, x => x);

		var roteirosPorDataInicio = roteiros
			.ToDictionary(r => r.DataInicio.ToString(dateFormatDict), r => r);

		//var vigenciaPorAlunoData = vigencias
		//	.ToDictionary(x => x.Aluno_Id, x => x);

		var cultura = new CultureInfo("pt-BR");

		//
		// Insere os meses do ano com seus respectivos roteiros
		//
		for (int mesIndex = 1; mesIndex < 13; mesIndex++)
		{
			var roteirosDoMes = roteiros
				.Where(roteiro =>
				{
					bool ehDoMes = roteiro.DataInicio.Month == mesIndex;
					bool inicioEhDezembro = roteiro.DataInicio.Month == 12;
					bool fimEhJaneiro = roteiro.DataFim.Month == 1;
					bool ehAnoAnterior = roteiro.DataInicio.Year == request.Ano - 1;
					bool ehAnoPosterior = roteiro.DataFim.Year == request.Ano + 1;

					bool ehInicioDoAno = inicioEhDezembro && fimEhJaneiro && ehAnoAnterior;
					bool ehFimDoAno = inicioEhDezembro && fimEhJaneiro && ehAnoPosterior;

					return (ehDoMes && !ehInicioDoAno && !ehFimDoAno)
					  || (!ehDoMes && ehInicioDoAno && !ehFimDoAno && mesIndex == 1)
					  || (ehDoMes && !ehInicioDoAno && ehFimDoAno && mesIndex == 12);
				})
			   .OrderBy(x => x.DataInicio)
			   .ToList();

			Monitoramento_Mes mes = new Monitoramento_Mes()
			{
				Mes = mesIndex,
				MesString = cultura.DateTimeFormat.GetMonthName(mesIndex),
				Roteiros = _mapper.Map<List<Monitoramento_Roteiro>>(roteirosDoMes),
			};
			response.MesesRoteiro.Add(mes);
		}


		foreach (AlunoList aluno in alunos)
		{
			var monitoramentoAluno = _mapper.Map<Monitoramento_Aluno>(aluno);

			foreach (RoteiroModel roteiro in roteiros)
			{
				var vigenciaAlunoRoteiro = vigencias
					.FirstOrDefault(x => x.Aluno_Id == aluno.Id
								&& x.DataInicioVigencia.Date <= roteiro.DataInicio.Date
								&& (!x.DataFimVigencia.HasValue || x.DataFimVigencia.Value.Date >= roteiro.DataFim.Date));

				DateTime dataTurma = roteiro.DataInicio.Date;
				var monitoramentoAlunoItem = new Monitoramento_Aluno_Item();
				var monitoramentoAula = new Monitoramento_Aula();
				var monitoramentoParticipacao = new Monitoramento_Participacao();
				Monitoramento_Aula_Participacao_Rel? monitoramentoReposicaoPara = null;

				if (vigenciaAlunoRoteiro is not null)
				{

					// Celula/Aula da vigencia do roteiro
					if (turmasPorId.TryGetValue(vigenciaAlunoRoteiro.Turma_Id, out var turma))
					{
						dataTurma = GetDataTurmaFromInterval(turma, roteiro.DataInicio, roteiro.DataFim);
						feriadosPorData.TryGetValue(dataTurma.ToString(dateFormatDict), out var feriado);

						//
						// Aula Instanciada
						//
						if (eventosPorTurmaData.TryGetValue((turma.Id, dataTurma.Date), out var aula))
						{
							monitoramentoAula = _mapper.Map<Monitoramento_Aula>(aula);
							monitoramentoAula.Feriado = feriado is null ? null : new Monitoramento_Feriado { Name = feriado.name, Date = feriado.date };
							monitoramentoAula.Tema = aula.Tema ?? roteiro.Tema;
							monitoramentoAula.Semana = aula.Semana ?? roteiro.Semana;
							monitoramentoAula.RoteiroCorLegenda = roteiro.CorLegenda;
							monitoramentoAula.Recesso = roteiro.Recesso;


							if (participacaoPorAlunoEvento.TryGetValue((aluno.Id, aula.Id), out var participacao))
							{
								monitoramentoParticipacao = _mapper.Map<Monitoramento_Participacao>(participacao);

								if (participacao.ReposicaoPara_Evento_Id.HasValue)
								{
									if (eventosPorId.TryGetValue(participacao.ReposicaoPara_Evento_Id.Value, out var reposicaoPara))
									{
										participacaoPorAlunoEvento.TryGetValue((aluno.Id, participacao.ReposicaoPara_Evento_Id.Value), out var participacaoReposicaoPara);
										feriadosPorData.TryGetValue(aula.Data.ToString(dateFormatDict), out var feriadoReposicaoPara);

										RoteiroModel? roteiroReposicaoPara;
										if (reposicaoPara.Roteiro_Id.HasValue)
										{
											roteirosPorId.TryGetValue(reposicaoPara.Roteiro_Id.Value, out var roteiroDict);
											roteiroReposicaoPara = roteiroDict;
										}
										else
										{
											roteirosPorDataInicio.TryGetValue(aula.Data.ToString(dateFormatDict), out var roteiroDict);
											roteiroReposicaoPara = roteiroDict;
										}

										monitoramentoReposicaoPara = new Monitoramento_Aula_Participacao_Rel();
										monitoramentoReposicaoPara.Aula = _mapper.Map<Monitoramento_Aula>(reposicaoPara);
										monitoramentoReposicaoPara.Aula.Feriado = feriadoReposicaoPara is null ? null : new Monitoramento_Feriado { Name = feriadoReposicaoPara.name, Date = feriadoReposicaoPara.date };
										monitoramentoReposicaoPara.Aula.Tema = reposicaoPara.Tema ?? roteiroReposicaoPara?.Tema ?? "Tema indefinido";
										monitoramentoReposicaoPara.Aula.Semana = reposicaoPara.Semana ?? roteiroReposicaoPara?.Semana ?? -1;
										monitoramentoReposicaoPara.Aula.RoteiroCorLegenda = roteiroReposicaoPara?.CorLegenda ?? "";
										monitoramentoReposicaoPara.Aula.Recesso = roteiroReposicaoPara?.Recesso ?? false;
										monitoramentoReposicaoPara.Participacao = _mapper.Map<Monitoramento_Participacao>(participacaoReposicaoPara);

									}

								}
							}
						}
						//
						// Pseudo Aula
						//
						else
						{
							monitoramentoAula = new Monitoramento_Aula
							{
								Id = -1,
								EventoTipo_Id = (int)EventoTipo.Aula,
								Data = dataTurma,
								Descricao = turma.Nome,
								Observacao = string.Empty,
								Finalizado = false,
								Sala = turma.Sala ?? "Sala Indefinida",
								Andar = turma.Andar ?? 0,
								NumeroSala = turma.NumeroSala ?? 0,
								Tema = roteiro.Tema,
								Semana = roteiro.Semana,
								Recesso = roteiro.Recesso,
								RoteiroCorLegenda = roteiro.CorLegenda,
								Turma = turma.Nome,
								Professor = turma.Professor ?? string.Empty,
								CorLegenda = turma.CorLegenda ?? string.Empty,
								Feriado = feriado is not null ? new Monitoramento_Feriado { Name = feriado.name, Date = feriado.date } : null,
							};
							monitoramentoParticipacao = new Monitoramento_Participacao
							{
								Id = -1,
								Apostila_Abaco = aluno.Apostila_Abaco ?? string.Empty,
								Apostila_AH = aluno.Apostila_AH ?? string.Empty,
								NumeroPaginaAbaco = aluno.NumeroPaginaAbaco,
								NumeroPaginaAH = aluno.NumeroPaginaAH,
							};
						}
							

					}
				}
				else if (turmasPorId.TryGetValue(aluno.Turma_Id!.Value, out var turma))
				{
					dataTurma = GetDataTurmaFromInterval(turma, roteiro.DataInicio, roteiro.DataFim);
					feriadosPorData.TryGetValue(dataTurma.ToString(dateFormatDict), out var feriado);

					monitoramentoAula = new Monitoramento_Aula
					{
						Id = -1,
						EventoTipo_Id = (int)EventoTipo.Aula,
						Data = dataTurma,
						Descricao = turma.Nome,
						Observacao = string.Empty,
						Finalizado = false,
						Sala = turma.Sala ?? "Sala Indefinida",
						Andar = turma.Andar ?? 0,
						NumeroSala = turma.NumeroSala ?? 0,
						Tema = roteiro.Tema,
						Semana = roteiro.Semana,
						Recesso = roteiro.Recesso,
						RoteiroCorLegenda = roteiro.CorLegenda,
						Turma = turma.Nome,
						Professor = turma.Professor ?? string.Empty,
						CorLegenda = turma.CorLegenda ?? string.Empty,
						Feriado = feriado is not null ? new Monitoramento_Feriado { Name = feriado.name, Date = feriado.date } : null,
					};
					monitoramentoParticipacao = new Monitoramento_Participacao
						{
							Id = -1,
							Apostila_Abaco = aluno.Apostila_Abaco ?? string.Empty,
							Apostila_AH = aluno.Apostila_AH ?? string.Empty,
							NumeroPaginaAbaco = aluno.NumeroPaginaAbaco,
							NumeroPaginaAH = aluno.NumeroPaginaAH,
						};
				}

				monitoramentoAlunoItem.Id = monitoramentoAula.Id;
				monitoramentoAlunoItem.Show = dataTurma.Year == request.Ano && roteiro.Recesso == false;
				monitoramentoAlunoItem.ReposicaoPara = monitoramentoReposicaoPara;
				monitoramentoAlunoItem.Aula = new Monitoramento_Aula_Participacao_Rel
				{
					Aula = monitoramentoAula,
					Participacao = monitoramentoParticipacao,
				};
				monitoramentoAluno.Items.Add(monitoramentoAlunoItem);
			
			}

			response.Alunos.Add(monitoramentoAluno);
		}

		return response;

		#region old
		////
		//// Insere aulas
		////
		//var aulasPorAluno = new Dictionary<int, List<Monitoramento_Aluno_Item>>();

		//foreach (RoteiroModel roteiro in roteiros)
		//{
		//	DateTime data = roteiro.DataInicio.Date;
		//	while (data <= roteiro.DataFim.Date)
		//	{
		//		var turmasDoDia = turmas.Where(x => x.DiaSemana == (int)data.DayOfWeek).ToList();
		//		bool dataDoAno = data.Year == request.Ano;

		//		foreach (TurmaList turma in turmasDoDia)
		//		{
		//			eventosPorTurmaData.TryGetValue((turma.Id, data.Date), out var eventoAula);

		//			//
		//			// Aulas Instanciadas
		//			//
		//			if (eventoAula is not null)
		//			{
		//				if (participacoesPorEvento.TryGetValue(eventoAula.Id, out var participacoesAula))
		//				{
		//					foreach (CalendarioAlunoList participacao in participacoesAula)
		//					{
		//						if (!participacao.ReposicaoDe_Evento_Id.HasValue)
		//						{
		//							feriadosPorData.TryGetValue(eventoAula.Data.ToString(""), out var feriado);

		//							var dashAula = _mapper.Map<Monitoramento_Aula>(eventoAula);

		//							dashAula.Feriado = feriado is null ? null : new Monitoramento_Feriado { Name = feriado.name, Date = feriado.date };
		//							dashAula.Tema = eventoAula.Tema ?? roteiro.Tema;
		//							dashAula.Semana = eventoAula.Semana ?? roteiro.Semana;
		//							dashAula.RoteiroCorLegenda = roteiro.CorLegenda;
		//							dashAula.Recesso = roteiro.Recesso;

		//							var dashPart = _mapper.Map<Monitoramento_Participacao>(participacao);

		//							var dashAulaPart = new Monitoramento_Aula_Participacao_Rel
		//							{
		//								Aula = dashAula,
		//								Participacao = dashPart,
		//							};

		//							var item = new Monitoramento_Aluno_Item
		//							{
		//								Id = participacao.Id,
		//								Show = dataDoAno && roteiro.Recesso == false,
		//								Aula = dashAulaPart,
		//								ReposicaoPara = null,
		//							};

		//							if (participacao.ReposicaoPara_Evento_Id.HasValue)
		//							{
		//								if (eventosPorId.TryGetValue(participacao.ReposicaoPara_Evento_Id.Value, out var aulaReposicaoPara))
		//								{
		//									participacaoPorAlunoEvento.TryGetValue((participacao.Aluno_Id, aulaReposicaoPara.Id), out var participacaoReposicaoPara);

		//									feriadosPorData.TryGetValue(eventoAula.Data.ToString(dateFormatDict), out var feriadoReposicaoPara);

		//									RoteiroModel? roteiroReposicaoPara;
		//									if (aulaReposicaoPara.Roteiro_Id.HasValue)
		//									{
		//										roteirosPorId.TryGetValue(aulaReposicaoPara.Roteiro_Id.Value, out var roteiroDict);
		//										roteiroReposicaoPara = roteiroDict;
		//									}
		//									else
		//									{
		//										roteirosPorDataInicio.TryGetValue(data.Date.ToString(dateFormatDict), out var roteiroDict);
		//										roteiroReposicaoPara = roteiroDict;
		//									}

		//									var reposicaoPara = new Monitoramento_Aula_Participacao_Rel();

		//									reposicaoPara.Aula = _mapper.Map<Monitoramento_Aula>(aulaReposicaoPara);
		//									reposicaoPara.Aula.Feriado = feriadoReposicaoPara is null ? null : new Monitoramento_Feriado { Name = feriadoReposicaoPara.name, Date = feriadoReposicaoPara.date };
		//									reposicaoPara.Aula.Tema = aulaReposicaoPara.Tema ?? roteiroReposicaoPara?.Tema ?? "Tema indefinido";
		//									reposicaoPara.Aula.Semana = aulaReposicaoPara.Semana ?? roteiroReposicaoPara?.Semana ?? -1;
		//									reposicaoPara.Aula.RoteiroCorLegenda = roteiroReposicaoPara?.CorLegenda ?? "";
		//									reposicaoPara.Aula.Recesso = roteiroReposicaoPara?.Recesso ?? false;

		//									reposicaoPara.Participacao = _mapper.Map<Monitoramento_Participacao>(participacaoReposicaoPara);

		//									item.ReposicaoPara = reposicaoPara;
		//								}
		//							}

		//							if (!aulasPorAluno.TryGetValue(participacao.Aluno_Id, out var list))
		//							{
		//								list = new List<Monitoramento_Aluno_Item>();
		//								aulasPorAluno[participacao.Aluno_Id] = list;
		//							}
		//							list.Add(item);
		//						}
		//					}
		//				}


		//			}
		//			//
		//			// Pseudo Aulas
		//			//
		//			else
		//			{
		//				var vigenciasDaTurma = vigencias
		//					.Where(x => x.Turma_Id == turma.Id
		//							&& data.Date >= x.DataInicioVigencia.Date
		//							&& (!x.DataFimVigencia.HasValue || data.Date <= x.DataFimVigencia.Value.Date))
		//					.ToList();

		//				var alunosDoDiaId = vigenciasDaTurma
		//					.Select(x => x.Aluno_Id)
		//					.ToList();

		//				var alunosTurma = alunos
		//					.Where(x => alunosDoDiaId.Contains(x.Id))
		//					.ToList();

		//				feriadosPorData.TryGetValue(data.Date.ToString(dateFormatDict), out var feriado);

		//				Monitoramento_Aula pseudoAula = new Monitoramento_Aula
		//				{
		//					Id = -1,
		//					EventoTipo_Id = (int)EventoTipo.Aula,
		//					Data = new DateTime(data.Year, data.Month, data.Day, turma!.Horario!.Value.Hours, turma.Horario.Value.Minutes, 0),
		//					Descricao = turma.Nome,
		//					Observacao = string.Empty,
		//					Finalizado = false,
		//					Sala = turma.Sala ?? "Sala Indefinida",
		//					Andar = turma.Andar ?? 0,
		//					NumeroSala = turma.NumeroSala ?? 0,
		//					Tema = roteiro.Tema,
		//					Semana = roteiro.Semana,
		//					Recesso = roteiro.Recesso,
		//					RoteiroCorLegenda = roteiro.CorLegenda,
		//					Turma = turma.Nome,
		//					Professor = turma.Professor ?? string.Empty,
		//					CorLegenda = turma.CorLegenda ?? string.Empty,
		//					Feriado = feriado is not null ? new Monitoramento_Feriado { Name = feriado.name, Date = feriado.date } : null,
		//				};

		//				foreach (var aluno in alunosTurma)
		//				{

		//					Monitoramento_Participacao dashPart = new Monitoramento_Participacao
		//					{
		//						Id = -1,
		//						Apostila_Abaco = aluno.Apostila_Abaco ?? string.Empty,
		//						Apostila_AH = aluno.Apostila_AH ?? string.Empty,
		//						NumeroPaginaAbaco = aluno.NumeroPaginaAbaco,
		//						NumeroPaginaAH = aluno.NumeroPaginaAH,
		//					};

		//					Monitoramento_Aula_Participacao_Rel dashAulaPart = new Monitoramento_Aula_Participacao_Rel
		//					{
		//						Aula = pseudoAula,
		//						Participacao = dashPart,
		//					};

		//					Monitoramento_Aluno_Item item = new Monitoramento_Aluno_Item
		//					{
		//						Id = -1,
		//						Show = dataDoAno && roteiro.Recesso == false,
		//						Aula = dashAulaPart,
		//						ReposicaoPara = null,
		//					};

		//					if (!aulasPorAluno.TryGetValue(aluno.Id, out var list))
		//					{
		//						list = new List<Monitoramento_Aluno_Item>();
		//						aulasPorAluno[aluno.Id] = list;
		//					}
		//					list.Add(item);
		//				}
		//			}
		//		}


		//		data = data.AddDays(1);
		//	}

		//}
		//var alunosList = alunos.Select(aluno =>
		//{
		//	var alunoModel = _mapper.Map<Monitoramento_Aluno>(aluno);
		//	if (aulasPorAluno.TryGetValue(aluno.Id, out var list))
		//	{
		//		alunoModel.Items = list.OrderBy(x => x.Aula.Aula.Data).ToList();
		//	}
		//	else
		//	{
		//		alunoModel.Items = new List<Monitoramento_Aluno_Item>();
		//	}
		//	return alunoModel;
		//})
		//.OrderBy(x => x.Turma_Id)
		//.ThenBy(x => x.Nome)
		//.ToList();

		//Monitoramento_Response response = new Monitoramento_Response
		//{
		//	MesesRoteiro = meses,
		//	Alunos = alunosList
		//};
		#endregion

	}

	public DateTime GetDataTurmaFromInterval(TurmaList turma, DateTime intervaloDe, DateTime intervaloAte)
	{
		var data = intervaloDe;

		while(turma.DiaSemana != (int)data.DayOfWeek)
		{
			data = data.AddDays(1);
		}

		data = new DateTime(data.Year, data.Month, data.Day, turma.Horario.Value.Hours, turma.Horario.Value.Minutes, 0);

		return data;

	}


}
