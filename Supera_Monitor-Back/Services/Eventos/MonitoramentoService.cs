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

namespace Supera_Monitor_Back.Services.Eventos;

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

		var eventosQueryable = _db.CalendarioEventoList
				.Where(x => x.Data.Date >= intervaloDe.Date && x.Data.Date <= intervaloAte.Date
							&& (x.Evento_Tipo_Id == (int)EventoTipo.Aula || x.Evento_Tipo_Id == (int)EventoTipo.AulaExtra));

		var participacoesQueryable = _db.CalendarioAlunoList
			.AsQueryable();

		var turmasQueryable = _db.TurmaLists
			.Where(t => t.Deactivated == null);


		var alunosQueryable = _db.AlunoList
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
						&& x.DataFimVigencia == null 
						|| (x.DataInicioVigencia.Year >= request.Ano && x.DataFimVigencia.Value.Year <= request.Ano))
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

		
		var participacaoPorAlunoEvento = participacoes
			.GroupBy(x => new { x.Aluno_Id, x.Evento_Id })
			.ToDictionary(g => (g.Key.Aluno_Id, g.Key.Evento_Id), g => g.First());

		string dateFormatDict = "ddMMyyyyHHmm";

		var alunosPorId = alunos
			.ToDictionary(x => x.Id, x => x);

		var feriadosPorData = feriados
			.ToDictionary(x => x.date.ToString(dateFormatDict), x => x);

		var roteirosPorId = roteiros.Where(x => x.Id != -1)
			.ToDictionary(r => r.Id, r => r);

		var turmasPorId = turmas
			.ToDictionary(x => x.Id, x => x);

		var roteirosPorDataInicio = roteiros
			.ToDictionary(r => r.DataInicio.ToString(dateFormatDict), r => r);

		var vigenciaPorAluno = vigencias
			.GroupBy(x => new { x.Aluno_Id })
			.ToDictionary(x => x.Key.Aluno_Id, x => x.ToList());

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

					return ehDoMes && !ehInicioDoAno && !ehFimDoAno
					  || !ehDoMes && ehInicioDoAno && !ehFimDoAno && mesIndex == 1
					  || ehDoMes && !ehInicioDoAno && ehFimDoAno && mesIndex == 12;
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

		#region foreach roteiros x foreach alunos

		var monitoramentosAlunos = _mapper.Map<List<Monitoramento_Aluno>>(alunos);


		foreach (RoteiroModel roteiro in roteiros)
		{
			foreach (var monitoramentoAluno in monitoramentosAlunos)
			{
				if (alunosPorId.TryGetValue(monitoramentoAluno.Id, out var aluno))
				{
					var vigenciasAluno = vigenciaPorAluno
						.GetValueOrDefault(aluno.Id, new List<Aluno_Turma_Vigencia>() { })
						.OrderBy(x => x.Id)
						.ToList();

					var vigenciaAlunoRoteiro = vigenciasAluno
						.FirstOrDefault(vigencia =>
							vigencia.DataInicioVigencia.Date <= roteiro.DataInicio.Date
							&& (vigencia.DataFimVigencia == null || roteiro.DataInicio.Date <= vigencia.DataFimVigencia.Value.Date)
						);

					//var vigencia1 = vigenciasAluno.First();
					//var vigencia2 = vigenciasAluno.Last();


					//var a1 = vigencia1.DataInicioVigencia.Date <= roteiro.DataInicio.Date;
					//var a2 = vigencia1.DataFimVigencia.HasValue;
					//var a3 = vigencia1.DataFimVigencia >= roteiro.DataInicio;
					//var a4 = vigencia1.DataFimVigencia <= roteiro.DataFim;
					//var a5 = !a2 || (a2 && (a2 || a4));


					//var a1 = vigencia1.DataInicioVigencia.Date <= roteiro.DataInicio.Date;
					//var a2 = vigencia1.DataFimVigencia.HasValue;
					//var a3 = (a2 && vigencia1.DataFimVigencia.Value.Date <= roteiro.DataFim.Date) || !a2;
					//var a4 = a1 && a3;

					//var b1 = vigencia2.DataInicioVigencia.Date <= roteiro.DataInicio.Date;
					//var b2 = vigencia2.DataFimVigencia.HasValue;
					//var b3 = (b2 && vigencia2.DataFimVigencia.Value.Date <= roteiro.DataFim.Date) || !b2;
					//var b4 = b1 && b3;





					DateTime dataTurma = roteiro.DataInicio.Date;
					var monitoramentoAlunoItem = new Monitoramento_Aluno_Item();
					var monitoramentoAula = new Monitoramento_Aula();
					var monitoramentoParticipacao = new Monitoramento_Participacao();

					FeriadoResponse? feriado = null;
					Monitoramento_Aula_Participacao_Rel? monitoramentoReposicaoPara = null;

					if (vigenciaAlunoRoteiro is not null)
					{

						// Celula/Aula da vigencia do roteiro
						if (turmasPorId.TryGetValue(vigenciaAlunoRoteiro.Turma_Id, out var turma))
						{
							dataTurma = GetDataTurmaFromInterval(turma, roteiro.DataInicio, roteiro.DataFim);
							feriadosPorData.TryGetValue(dataTurma.ToString(dateFormatDict), out feriado);

							//
							// Aula Instanciada
							//
							if (eventosPorTurmaData.TryGetValue((turma.Id, dataTurma.Date), out var aula))
							{
								monitoramentoAula = _mapper.Map<Monitoramento_Aula>(aula);
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
												//roteirosPorDataInicio.TryGetValue(aula.Data.ToString(dateFormatDict), out var roteiroDict);
												var aulaData = aula.Data.Date;
												roteiroReposicaoPara = roteiros.FirstOrDefault(x => aulaData >= x.DataInicio && aulaData <= x.DataFim);
											}

											monitoramentoReposicaoPara = new Monitoramento_Aula_Participacao_Rel();
											monitoramentoReposicaoPara.Aula = _mapper.Map<Monitoramento_Aula>(reposicaoPara);
											monitoramentoReposicaoPara.Aula.Tema = reposicaoPara.Tema ?? roteiroReposicaoPara?.Tema ?? "Tema indefinido";
											monitoramentoReposicaoPara.Aula.Semana = reposicaoPara.Semana ?? roteiroReposicaoPara?.Semana ?? -1;
											monitoramentoReposicaoPara.Aula.RoteiroCorLegenda = roteiroReposicaoPara?.CorLegenda ?? "";
											monitoramentoReposicaoPara.Aula.Recesso = roteiroReposicaoPara?.Recesso ?? false;
											monitoramentoReposicaoPara.Participacao = _mapper.Map<Monitoramento_Participacao>(participacaoReposicaoPara);

											if (feriadoReposicaoPara is not null && monitoramentoReposicaoPara.Aula.Active == true)
											{

												monitoramentoReposicaoPara.Aula.Feriado = _mapper.Map<Monitoramento_Feriado>(feriadoReposicaoPara);
												monitoramentoReposicaoPara.Aula.Observacao = "Cancelamento Automático. <br> Feriado: " + feriadoReposicaoPara.name;
												monitoramentoReposicaoPara.Aula.Deactivated = feriadoReposicaoPara.date;
											}
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
						feriadosPorData.TryGetValue(dataTurma.ToString(dateFormatDict), out feriado);

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

					if (feriado is not null && monitoramentoAula.Active)
					{
						monitoramentoAula.Feriado = _mapper.Map<Monitoramento_Feriado>(feriado);
						monitoramentoAula.Observacao = "Cancelamento Automático. <br> Feriado: " + feriado.name;
						monitoramentoAula.Deactivated = feriado.date;
					}

					monitoramentoAlunoItem.Id = monitoramentoAula.Id;
					monitoramentoAlunoItem.Show = 
							feriado is null
							&& dataTurma.Year == request.Ano 
							&& roteiro.Recesso == false
							&& vigenciaAlunoRoteiro is not null;
					monitoramentoAlunoItem.ReposicaoPara = monitoramentoReposicaoPara;
					monitoramentoAlunoItem.Aula = new Monitoramento_Aula_Participacao_Rel
					{
						Aula = monitoramentoAula,
						Participacao = monitoramentoParticipacao,
					};
					monitoramentoAluno.Items.Add(monitoramentoAlunoItem);

				}


			}
		}
		response.Alunos = monitoramentosAlunos;

		#endregion

		#region foreach alunos x foreach roteiros

		//foreach (AlunoList aluno in alunos)
		//{
		//	var monitoramentoAluno = _mapper.Map<Monitoramento_Aluno>(aluno);

		//	var vigenciasAluno = vigenciaPorAluno.GetValueOrDefault(aluno.Id, new List<Aluno_Turma_Vigencia>() { }) ;

		//	foreach (RoteiroModel roteiro in roteiros)
		//	{
		//		var vigenciaAlunoRoteiro = vigenciasAluno
		//				.FirstOrDefault(x => x.DataInicioVigencia.Date <= roteiro.DataInicio.Date
		//							&& (!x.DataFimVigencia.HasValue || x.DataFimVigencia.Value.Date >= roteiro.DataFim.Date));

		//		DateTime dataTurma = roteiro.DataInicio.Date;
		//		var monitoramentoAlunoItem = new Monitoramento_Aluno_Item();
		//		var monitoramentoAula = new Monitoramento_Aula();
		//		var monitoramentoParticipacao = new Monitoramento_Participacao();
		//		Monitoramento_Aula_Participacao_Rel? monitoramentoReposicaoPara = null;

		//		if (vigenciaAlunoRoteiro is not null)
		//		{

		//			// Celula/Aula da vigencia do roteiro
		//			if (turmasPorId.TryGetValue(vigenciaAlunoRoteiro.Turma_Id, out var turma))
		//			{
		//				dataTurma = GetDataTurmaFromInterval(turma, roteiro.DataInicio, roteiro.DataFim);
		//				feriadosPorData.TryGetValue(dataTurma.ToString(dateFormatDict), out var feriado);

		//				//
		//				// Aula Instanciada
		//				//
		//				if (eventosPorTurmaData.TryGetValue((turma.Id, dataTurma.Date), out var aula))
		//				{
		//					monitoramentoAula = _mapper.Map<Monitoramento_Aula>(aula);
		//					monitoramentoAula.Feriado = feriado is null ? null : new Monitoramento_Feriado { Name = feriado.name, Date = feriado.date };
		//					monitoramentoAula.Tema = aula.Tema ?? roteiro.Tema;
		//					monitoramentoAula.Semana = aula.Semana ?? roteiro.Semana;
		//					monitoramentoAula.RoteiroCorLegenda = roteiro.CorLegenda;
		//					monitoramentoAula.Recesso = roteiro.Recesso;


		//					if (participacaoPorAlunoEvento.TryGetValue((aluno.Id, aula.Id), out var participacao))
		//					{
		//						monitoramentoParticipacao = _mapper.Map<Monitoramento_Participacao>(participacao);

		//						if (participacao.ReposicaoPara_Evento_Id.HasValue)
		//						{
		//							if (eventosPorId.TryGetValue(participacao.ReposicaoPara_Evento_Id.Value, out var reposicaoPara))
		//							{
		//								participacaoPorAlunoEvento.TryGetValue((aluno.Id, participacao.ReposicaoPara_Evento_Id.Value), out var participacaoReposicaoPara);
		//								feriadosPorData.TryGetValue(aula.Data.ToString(dateFormatDict), out var feriadoReposicaoPara);

		//								RoteiroModel? roteiroReposicaoPara;
		//								if (reposicaoPara.Roteiro_Id.HasValue)
		//								{
		//									roteirosPorId.TryGetValue(reposicaoPara.Roteiro_Id.Value, out var roteiroDict);
		//									roteiroReposicaoPara = roteiroDict;
		//								}
		//								else
		//								{
		//									roteirosPorDataInicio.TryGetValue(aula.Data.ToString(dateFormatDict), out var roteiroDict);
		//									roteiroReposicaoPara = roteiroDict;
		//								}

		//								monitoramentoReposicaoPara = new Monitoramento_Aula_Participacao_Rel();
		//								monitoramentoReposicaoPara.Aula = _mapper.Map<Monitoramento_Aula>(reposicaoPara);
		//								monitoramentoReposicaoPara.Aula.Feriado = feriadoReposicaoPara is null ? null : new Monitoramento_Feriado { Name = feriadoReposicaoPara.name, Date = feriadoReposicaoPara.date };
		//								monitoramentoReposicaoPara.Aula.Tema = reposicaoPara.Tema ?? roteiroReposicaoPara?.Tema ?? "Tema indefinido";
		//								monitoramentoReposicaoPara.Aula.Semana = reposicaoPara.Semana ?? roteiroReposicaoPara?.Semana ?? -1;
		//								monitoramentoReposicaoPara.Aula.RoteiroCorLegenda = roteiroReposicaoPara?.CorLegenda ?? "";
		//								monitoramentoReposicaoPara.Aula.Recesso = roteiroReposicaoPara?.Recesso ?? false;
		//								monitoramentoReposicaoPara.Participacao = _mapper.Map<Monitoramento_Participacao>(participacaoReposicaoPara);

		//							}

		//						}
		//					}
		//				}
		//				//
		//				// Pseudo Aula
		//				//
		//				else
		//				{
		//					monitoramentoAula = new Monitoramento_Aula
		//					{
		//						Id = -1,
		//						EventoTipo_Id = (int)EventoTipo.Aula,
		//						Data = dataTurma,
		//						Descricao = turma.Nome,
		//						Observacao = string.Empty,
		//						Finalizado = false,
		//						Sala = turma.Sala ?? "Sala Indefinida",
		//						Andar = turma.Andar ?? 0,
		//						NumeroSala = turma.NumeroSala ?? 0,
		//						Tema = roteiro.Tema,
		//						Semana = roteiro.Semana,
		//						Recesso = roteiro.Recesso,
		//						RoteiroCorLegenda = roteiro.CorLegenda,
		//						Turma = turma.Nome,
		//						Professor = turma.Professor ?? string.Empty,
		//						CorLegenda = turma.CorLegenda ?? string.Empty,
		//						Feriado = feriado is not null ? new Monitoramento_Feriado { Name = feriado.name, Date = feriado.date } : null,
		//					};
		//					monitoramentoParticipacao = new Monitoramento_Participacao
		//					{
		//						Id = -1,
		//						Apostila_Abaco = aluno.Apostila_Abaco ?? string.Empty,
		//						Apostila_AH = aluno.Apostila_AH ?? string.Empty,
		//						NumeroPaginaAbaco = aluno.NumeroPaginaAbaco,
		//						NumeroPaginaAH = aluno.NumeroPaginaAH,
		//					};
		//				}


		//			}
		//		}
		//		else if (turmasPorId.TryGetValue(aluno.Turma_Id!.Value, out var turma))
		//		{
		//			dataTurma = GetDataTurmaFromInterval(turma, roteiro.DataInicio, roteiro.DataFim);
		//			feriadosPorData.TryGetValue(dataTurma.ToString(dateFormatDict), out var feriado);

		//			monitoramentoAula = new Monitoramento_Aula
		//			{
		//				Id = -1,
		//				EventoTipo_Id = (int)EventoTipo.Aula,
		//				Data = dataTurma,
		//				Descricao = turma.Nome,
		//				Observacao = string.Empty,
		//				Finalizado = false,
		//				Sala = turma.Sala ?? "Sala Indefinida",
		//				Andar = turma.Andar ?? 0,
		//				NumeroSala = turma.NumeroSala ?? 0,
		//				Tema = roteiro.Tema,
		//				Semana = roteiro.Semana,
		//				Recesso = roteiro.Recesso,
		//				RoteiroCorLegenda = roteiro.CorLegenda,
		//				Turma = turma.Nome,
		//				Professor = turma.Professor ?? string.Empty,
		//				CorLegenda = turma.CorLegenda ?? string.Empty,
		//				Feriado = feriado is not null ? new Monitoramento_Feriado { Name = feriado.name, Date = feriado.date } : null,
		//			};
		//			monitoramentoParticipacao = new Monitoramento_Participacao
		//			{
		//				Id = -1,
		//				Apostila_Abaco = aluno.Apostila_Abaco ?? string.Empty,
		//				Apostila_AH = aluno.Apostila_AH ?? string.Empty,
		//				NumeroPaginaAbaco = aluno.NumeroPaginaAbaco,
		//				NumeroPaginaAH = aluno.NumeroPaginaAH,
		//			};
		//		}

		//		monitoramentoAlunoItem.Id = monitoramentoAula.Id;
		//		monitoramentoAlunoItem.Show = dataTurma.Year == request.Ano && roteiro.Recesso == false;
		//		monitoramentoAlunoItem.ReposicaoPara = monitoramentoReposicaoPara;
		//		monitoramentoAlunoItem.Aula = new Monitoramento_Aula_Participacao_Rel
		//		{
		//			Aula = monitoramentoAula,
		//			Participacao = monitoramentoParticipacao,
		//		};
		//		monitoramentoAluno.Items.Add(monitoramentoAlunoItem);

		//	}

		//	response.Alunos.Add(monitoramentoAluno);
		//}

		#endregion
		return response;

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
