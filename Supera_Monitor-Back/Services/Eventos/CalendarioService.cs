	using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Eventos;
using Supera_Monitor_Back.Models.Roteiro;

namespace Supera_Monitor_Back.Services.Eventos;

public interface ICalendarioService
{
	public CalendarioResponse GetCalendario(CalendarioRequest request);
}

public class CalendarioService : ICalendarioService
{
	private readonly DataContext _db;
	private readonly IMapper _mapper;
	private readonly IRoteiroService _roteiroService;
	private readonly IEventoService _eventoService;
	private readonly ISalaService _salaService;
	private readonly IFeriadoService _feriadoService;


	public CalendarioService(
		DataContext db,
		IMapper mapper,
		IRoteiroService roteiroService,
		IEventoService eventoService,
		ISalaService salaService,
		IFeriadoService feriadoService
	)
	{
		_db = db;
		_mapper = mapper;
		_roteiroService = roteiroService;
		_salaService = salaService;
		_eventoService = eventoService;
		_feriadoService = feriadoService;
	}

	public CalendarioResponse GetCalendario(CalendarioRequest request)
	{

		DateTime now = TimeFunctions.HoraAtualBR();

		request.IntervaloDe ??= GetThisWeeksMonday(now); // Se não passar data inicio, considera a segunda-feira da semana atual
		request.IntervaloAte ??= GetThisWeeksSaturday((DateTime)request.IntervaloDe); // Se não passar data fim, considera o sábado da semana da data inicio

		if (request.IntervaloAte < request.IntervaloDe)
		{
			throw new Exception("Final do intervalo não pode ser antes do seu próprio início");
		}

		var eventosQueryable = _db.CalendarioEventoList
			.Where(e => e.Data.Date >= request.IntervaloDe.Value.Date && e.Data.Date <= request.IntervaloAte.Value.Date);

		var alunosQueryable = _db.AlunoList.AsQueryable();

		var professoresQueryable = _db.ProfessorList
			.Where(x => x.Active == true);

		var turmasQueryable = _db.TurmaLists
			.Where(x => x.Active == true);


		if (request.Perfil_Cognitivo_Id.HasValue)
		{
			eventosQueryable =
				from e in eventosQueryable
				join ep in _db.Evento_Aula_PerfilCognitivo_Rel
					on e.Id equals ep.Evento_Aula_Id
				where ep.PerfilCognitivo_Id == request.Perfil_Cognitivo_Id
				select e;

			turmasQueryable =
				from t in turmasQueryable
				join tp in _db.Turma_PerfilCognitivo_Rels
					on t.Id equals tp.Turma_Id
				where tp.PerfilCognitivo_Id == request.Perfil_Cognitivo_Id
				select t;

		}

		if (request.Turma_Id.HasValue)
		{
			eventosQueryable = 
				from e in eventosQueryable
				where e.Turma_Id == request.Turma_Id
				select e;

			turmasQueryable =
				from t in turmasQueryable
				where t.Id == request.Turma_Id
				select t;
		}

		if (request.Professor_Id.HasValue)
		{
			eventosQueryable = 
				from e in eventosQueryable
				join p in _db.Evento_Participacao_Professor
					on e.Id equals p.Evento_Id 
				where p.Professor_Id == request.Professor_Id
				select e;

			turmasQueryable =
				from t in turmasQueryable
				join p in _db.Professor
					on t.Professor_Id equals p.Id
				where p.Id == request.Professor_Id
				select t;

			professoresQueryable = 
				from p in professoresQueryable
				where p.Id == request.Professor_Id
				select p;
		}

		if (request.Aluno_Id.HasValue)
		{
			eventosQueryable =
				from e in eventosQueryable
				join a in _db.Evento_Participacao_Aluno
					on e.Id equals a.Evento_Id
				where a.Aluno_Id == request.Aluno_Id
				select e;

			turmasQueryable =
				from t in turmasQueryable
				join a in _db.Aluno
					on t.Id equals a.Turma_Id
				where a.Id == request.Aluno_Id
				select t;
		}

		var turmasIds = turmasQueryable
			.AsNoTracking()
			.Select(x => x.Id);

		var alunosIds = alunosQueryable
			.AsNoTracking()
			.Select(x => x.Id);

		var eventos = eventosQueryable
			.AsNoTracking()
			.ToList();

		var alunos = alunosQueryable
			.AsNoTracking()
			.ToList();

		var turmas = turmasQueryable
			.AsNoTracking()
			.ToList();

		var professores = professoresQueryable
			.AsNoTracking()
			.ToList();

		var vigencias = _db.Aluno_Turma_Vigencia
			.Where(x => turmasIds.Contains(x.Turma_Id) && alunosIds.Contains(x.Aluno_Id))
			.AsNoTracking()
			.ToList();

		var perfisCognitivosTurmas = _db.Turma_PerfilCognitivo_Rels
			.Where(x => turmasIds.Contains(x.Turma_Id))
			.Include(x => x.PerfilCognitivo)
			.AsNoTracking()
			.ToList();


		#region roteiro e feriados
		var anoDe = request.IntervaloDe.Value.Year;
		var anoAte = request.IntervaloAte.Value.Year;

		var roteiros = _roteiroService.GetAll(anoDe);
		var feriados = _feriadoService.GetList();
		
		if (anoDe != anoAte)
		{
			var roteiros2 = _roteiroService.GetAll(anoAte);
			roteiros.AddRange(roteiros2);
		}

		roteiros = roteiros
			.GroupBy(x => (x.Id, x.DataInicio, x.DataFim))
			.Select(x => x.First())
			.ToList();

		#endregion roteiro e feriados

		var eventosResponse = eventos;

		PopulateCalendarioEvents(eventosResponse, feriados, roteiros);

		DateTime data = request.IntervaloDe.Value.Date;

		string formatDateDict = "ddMMyyyyHHmm";

		var alunosPorId = alunos
			.ToDictionary(x => x.Id, x => x);

		var turmasPorDiaSemana = turmas
			.GroupBy(x => x.DiaSemana)
			.ToDictionary(x => x.Key, x => x.ToList());

		var eventosByTurmaData = eventos
			.GroupBy(x => (x.Turma_Id, x.Data.ToString(formatDateDict) ))
			.ToDictionary(x => x.Key, x => x.First());

		var vigenciasPorTurma = vigencias
			.GroupBy(x => x.Turma_Id)
			.ToDictionary(x => x.Key, x => x.ToList());

		var perfilPorTurma = perfisCognitivosTurmas
			.GroupBy(x => x.Turma_Id)
			.ToDictionary(x => x.Key, x => x.Select(x => x.PerfilCognitivo).ToList());

		var professoresPorId = professores
			.ToDictionary(x => x.Id, x => x);

		var feriadosPorData = feriados
			.ToDictionary(x => x.Data.ToString(formatDateDict), x => x);

		var roteirosPorData = roteiros
			.ToDictionary(x => x.DataInicio.ToString(formatDateDict), x => x);

		while (data <= request.IntervaloAte.Value)
		{
			var diaSemana = data.DayOfWeek;

			roteirosPorData.TryGetValue(data.ToString(formatDateDict), out var roteiroDoDia);
			feriadosPorData.TryGetValue(data.ToString(formatDateDict), out var feriadoNoDia);

			//
			// Adiciona Oficina - Se a já existe uma oficina agendada para segunda-feira, não vai adicionar
			//
			#region Adiciona Oficina
			
			if (diaSemana == DayOfWeek.Monday)
			{
				CalendarioEventoList? eventoOficina = eventosResponse
					.FirstOrDefault(x => x.Data.Date == data.Date 
											&& x.Evento_Tipo_Id == (int)EventoTipo.Oficina);

				if (eventoOficina is null)
				{
					CalendarioEventoList pseudoOficina = new()
					{
						Id = -1,
						Evento_Tipo_Id = (int)EventoTipo.Oficina,
						Evento_Tipo = "Pseudo-Oficina",
						CapacidadeMaximaEvento = 12,
						VagasDisponiveisEvento = 12,
						AlunosAtivosEvento = 0,

						Descricao = "Oficina - Tema indefinido",
						DuracaoMinutos = 60,

						Roteiro_Id = roteiroDoDia?.Id,
						Semana = roteiroDoDia?.Semana,
						Tema = roteiroDoDia?.Tema,
						RoteiroCorLegenda = roteiroDoDia?.CorLegenda,

						Data = new DateTime(data.Year, data.Month, data.Day, 10, 0, 0),
						Finalizado = false,
						Sala_Id = null,
						Sala = "Sala Indefinida",

						Feriado = feriadoNoDia,
						Deactivated = feriadoNoDia is null ? null : feriadoNoDia.Data,
						Observacao = feriadoNoDia is null ? null : "Cancelamento Automático. <br> Feriado: " + feriadoNoDia.Descricao,

					};

					eventosResponse.Add(pseudoOficina);
				}
			}
			
			#endregion
			
			//
			// Adiciona Reunião - Se a já existe uma reunião agendada para a data, não vai adicionar
			//
			#region Adiciona Reuniao  

			var diasReuniao = new DayOfWeek[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Friday };
			if (diasReuniao.Contains(diaSemana)) 
			{
				CalendarioEventoList? eventoReuniao = eventosResponse
														.FirstOrDefault(x => x.Data.Date == data.Date 
																		&& x.Evento_Tipo_Id == (int)EventoTipo.Reuniao);

				// Não usar mais o continue porque o método adiciona outros pseudo eventos
				if (eventoReuniao is null)
				{
					var descricao = diaSemana == DayOfWeek.Monday ? "Reunião Geral" : // Segunda-feira
									diaSemana == DayOfWeek.Tuesday ? "Reunião Monitoramento" : // Terça-feira
									"Reunião Pedagógica"; // Sexta-feira

					CalendarioEventoList pseudoReuniao = new()
					{
						Id = -1,
						Evento_Tipo_Id = (int)EventoTipo.Reuniao,
						Evento_Tipo = "Pseudo-Reuniao",
						Data = new DateTime(data.Year, data.Month, data.Day, 12, 0, 0),
						Descricao = descricao,
						DuracaoMinutos = 60,
						Finalizado = false,
						Sala_Id = (int)SalaAulaId.SalaPedagogica,
						Sala = "Sala Pedagógica",

						Roteiro_Id = roteiroDoDia?.Id,
						Semana = roteiroDoDia?.Semana,
						Tema = roteiroDoDia?.Tema,
						RoteiroCorLegenda = roteiroDoDia?.CorLegenda,

						Feriado = feriadoNoDia,
						Deactivated = feriadoNoDia is null ? null : feriadoNoDia.Data,
						Observacao = feriadoNoDia is null ? null : "Cancelamento Automático. <br> Feriado: " + feriadoNoDia.Descricao,

						Professores = professores.Select(professor => new CalendarioProfessorList
						{
							Evento_Id = -1,
							Professor_Id = professor.Id,
							Nome = professor.Nome,
							CorLegenda = professor.CorLegenda,
							Account_Id = professor.Account_Id,
							Telefone = professor.Telefone,
							ExpedienteFim = professor.ExpedienteFim,
							ExpedienteInicio = professor.ExpedienteInicio,
						}).ToList()
					};

					eventosResponse.Add(pseudoReuniao);
				}
			}

			#endregion


			var turmasDoDia = turmasPorDiaSemana.GetValueOrDefault((int)data.DayOfWeek, new List<TurmaList>());

			foreach (TurmaList turma in turmasDoDia)
			{
				DateTime dataTurma = new DateTime(data.Year, data.Month, data.Day, turma.Horario!.Value.Hours, turma.Horario!.Value.Minutes, 0);

				if (!eventosByTurmaData.TryGetValue((turma.Id, dataTurma.ToString(formatDateDict)), out var eventoAula))
				{
					var vigenciasDaTurma = vigenciasPorTurma.GetValueOrDefault(turma.Id, new List<Aluno_Turma_Vigencia>())
						.Where(x => x.DataInicioVigencia <= data
								&& (!x.DataFimVigencia.HasValue || x.DataFimVigencia.Value >= data))
						.ToList();

					var alunosDoDia =
						from a in alunos
						join v in vigenciasDaTurma
							on a.Id equals v.Aluno_Id
						select a;

					var alunosTurma = alunosDoDia.ToList();

					int alunosAtivosInTurma = alunosTurma.Count;

					var pseudoAula = new CalendarioEventoList()
					{
						Id = -1,
						Evento_Tipo_Id = (int)EventoTipo.Aula,
						Evento_Tipo = "Pseudo-Aula",

						Descricao = turma.Nome, // Pseudo aulas ganham o nome da turma
						DuracaoMinutos = 120, // As pseudo aulas são de uma turma e duram 2h por padrão

						Roteiro_Id = roteiroDoDia?.Id,
						Semana = roteiroDoDia?.Semana,
						Tema = roteiroDoDia?.Tema,
						RoteiroCorLegenda = roteiroDoDia?.CorLegenda,

						Data = dataTurma,

						Turma_Id = turma.Id,
						Turma = turma.Nome,

						VagasDisponiveisTurma = turma.CapacidadeMaximaAlunos - alunosAtivosInTurma,
						CapacidadeMaximaTurma = turma.CapacidadeMaximaAlunos,
						AlunosAtivosTurma = alunosAtivosInTurma,

						VagasDisponiveisEvento = turma.CapacidadeMaximaAlunos - alunosAtivosInTurma,
						CapacidadeMaximaEvento = turma.CapacidadeMaximaAlunos,
						AlunosAtivosEvento = alunosAtivosInTurma,

						Professor_Id = turma.Professor_Id,
						Professor = turma.Professor ?? "Professor indefinido",
						CorLegenda = turma.CorLegenda ?? "#000",

						Finalizado = false,
						Sala = turma.Sala ?? "Sala Indefinida",
						Sala_Id = turma.Sala_Id,
						NumeroSala = turma.NumeroSala,
						Andar = turma.Andar,

						Feriado = feriadoNoDia,
						Deactivated = feriadoNoDia is null ? null : feriadoNoDia.Data,
						Observacao = feriadoNoDia is null ? null : "Cancelamento Automático. <br> Feriado: " + feriadoNoDia.Descricao,

						//Alunos = _mapper.Map<List<CalendarioAlunoList>>(alunos)
						//						.OrderBy(a => a.Aluno)	
						//						.ToList(),
						Alunos = new List<CalendarioAlunoList>(),
					};

					var perfilEvento = perfilPorTurma.GetValueOrDefault(turma.Id, new List<PerfilCognitivo>());
					pseudoAula.PerfilCognitivo = _mapper.Map<List<PerfilCognitivoModel>>(perfilEvento);

					if (professoresPorId.TryGetValue(turma.Professor_Id!.Value, out var professor))
					{
						pseudoAula.Professores = new List<CalendarioProfessorList>()
						{
							new CalendarioProfessorList
								{
									Id = null,
									Evento_Id = -1,
									Professor_Id = turma.Professor_Id ?? -1,
									Nome = turma.Professor,
									CorLegenda = turma.CorLegenda ?? "#000",
									Presente = null,
									Observacao = "",
									Account_Id = professor?.Account_Id ?? -1,
									Telefone = professor?.Telefone ?? "",
									ExpedienteInicio = professor?.ExpedienteInicio,
									ExpedienteFim = professor.ExpedienteFim,
								}
						};
					}

					var pseudoParticipacoes = alunosTurma
						.OrderBy(x => x.Nome)
						.Select(aluno => new CalendarioAlunoList()
						{
							Id = -1,
							Evento_Id = -1,
							Aluno_Id = aluno.Id,
							Checklist = aluno.Checklist,
							Checklist_Id = aluno.Checklist_Id,
							Aluno = aluno.Nome,
							DataNascimento = aluno.DataNascimento,
							Celular = aluno.Celular,
							Aluno_Foto = null,
							Turma_Id = aluno.Turma_Id,
							Turma = aluno.Turma,
							PrimeiraAula = false,
							PrimeiraAula_Id = aluno.PrimeiraAula_Id,
							AulaZero_Id = aluno.AulaZero_Id,
							RestricaoMobilidade = aluno.RestricaoMobilidade,
							ReposicaoDe_Evento_Id = null,
							ReposicaoPara_Evento_Id = null,
							Presente = null,
							Apostila_Kit_Id = aluno.Apostila_Kit_Id,
							Kit = aluno.Kit,
							Apostila_Abaco = aluno.Apostila_Abaco,
							Apostila_AH = aluno.Apostila_AH,
							Apostila_Abaco_Id = aluno.Apostila_Abaco_Id,
							Apostila_AH_Id = aluno.Apostila_AH_Id,
							NumeroPaginaAbaco = aluno.NumeroPaginaAbaco,
							NumeroPaginaAH = aluno.NumeroPaginaAH,
							Observacao = null,
							Deactivated = null,
							AlunoContactado = null,
							ContatoObservacao = null,
							StatusContato_Id = null,
							PerfilCognitivo_Id = aluno.PerfilCognitivo_Id,
							PerfilCognitivo = aluno.PerfilCognitivo,
						})
						.ToList();

					pseudoAula.Alunos = pseudoParticipacoes;

					eventosResponse.Add(pseudoAula);
				}

			}

			data = data.AddDays(1);
		}

		var response = new CalendarioResponse
		{
			Eventos = eventosResponse,
			Feriados = feriados,
		};

		return response;
	}

	private static DateTime GetThisWeeksMonday(DateTime date)
	{
		var response = date.AddDays(-(int)date.DayOfWeek);
		return response.AddDays(1);
	}

	private static DateTime GetThisWeeksSaturday(DateTime date)
	{
		var response = date.AddDays(-(int)date.DayOfWeek);
		return response.AddDays(6);
	}

	private List<CalendarioEventoList> PopulateCalendarioEvents(List<CalendarioEventoList> events, List<FeriadoList> feriados, List<RoteiroModel> roteiros)
	{
		var eventoIds = events.Select(e => e.Id);

		var eventoAulaIds = events
			//.Where(e => e.Evento_Tipo_Id == (int)EventoTipo.Aula || e.Evento_Tipo_Id == (int)EventoTipo.AulaExtra)
			.Select(e => e.Id);

		var allAlunos = _db.CalendarioAlunoList
			.Where(x => eventoAulaIds.Contains(x.Evento_Id))
			.ToList();

		List<CalendarioProfessorList> allProfessores = _db.CalendarioProfessorLists
			.Where(p => eventoIds.Contains(p.Evento_Id))
			.ToList();

		List<Evento_Aula_PerfilCognitivo_Rel> allRels = _db.Evento_Aula_PerfilCognitivo_Rel
		   .AsNoTracking()
		   .Where(r => eventoAulaIds.Contains(r.Evento_Aula_Id))
		   .Include(r => r.PerfilCognitivo)
		   .ToList();

		// Create dictionaries that group alunos / professores by Evento_Id, will improve lookup performance
		// Key: GroupBy key Value: List of items in that group 

		var alunosDictionary = allAlunos
			.GroupBy(p => p.Evento_Id)
			.ToDictionary(g => g.Key, g => g.ToList());

		var professorDictionary = allProfessores
			.GroupBy(p => p.Evento_Id)
			.ToDictionary(g => g.Key, g => g.ToList());

		var perfisDictionary = allRels
			.GroupBy(r => r.Evento_Aula_Id)
			.ToDictionary( g => g.Key, g => _mapper.Map<List<PerfilCognitivoModel>>(g.Select(r => r.PerfilCognitivo)));

		string dateDictFormat = "ddMMyyyy";

		var feriadosDictionary = feriados
			.ToDictionary(x => x.Data.ToString(dateDictFormat), x => x);

		foreach (var calendarioEvent in events)
		{
			alunosDictionary.TryGetValue(calendarioEvent.Id, out var alunosInEvent);
			calendarioEvent.Alunos = alunosInEvent ?? new List<CalendarioAlunoList>();

			professorDictionary.TryGetValue(calendarioEvent.Id, out var professoresInEvent);
			calendarioEvent.Professores = professoresInEvent ?? new List<CalendarioProfessorList>();

			perfisDictionary.TryGetValue(calendarioEvent.Id, out var aulaPerfisInEvent);
			calendarioEvent.PerfilCognitivo = aulaPerfisInEvent ?? new List<PerfilCognitivoModel>();

			feriadosDictionary.TryGetValue(calendarioEvent.Data.ToString(dateDictFormat), out var feriadoInEvent);
			calendarioEvent.Feriado = feriadoInEvent;
			if (feriadoInEvent is not null && calendarioEvent.Active == true)
			{
				calendarioEvent.Deactivated = feriadoInEvent is null ? null : feriadoInEvent.Data;
				calendarioEvent.Observacao = feriadoInEvent is null ? null : "Cancelamento Automático. <br> Feriado: " + feriadoInEvent.Descricao;
			}

			if (calendarioEvent.Roteiro_Id == null) 
			{
				var aulaData = calendarioEvent.Data.Date;
				var roteiro = roteiros.FirstOrDefault(x => aulaData >= x.DataInicio && aulaData <= x.DataFim);

				calendarioEvent.Roteiro_Id = roteiro?.Id ?? -1;
				calendarioEvent.Semana = roteiro?.Semana;
				calendarioEvent.Tema = roteiro?.Tema;
				calendarioEvent.RoteiroCorLegenda = roteiro?.CorLegenda;
			}
		}

		return events;
	}

}
