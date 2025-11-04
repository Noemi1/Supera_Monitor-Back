using System.Globalization;
using System.Text.Json;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Dashboard;
using Supera_Monitor_Back.Models.Eventos;
using Supera_Monitor_Back.Models.Eventos.Dtos;
using Supera_Monitor_Back.Models.Eventos.Participacao;
using Supera_Monitor_Back.Models.Roteiro;

namespace Supera_Monitor_Back.Services.Eventos;

public interface ICalendarioService
{
	public List<CalendarioEventoList> GetCalendario(CalendarioRequest request);
}

public class CalendarioService : ICalendarioService
{
	private readonly DataContext _db;
	private readonly IMapper _mapper;
	private readonly IRoteiroService _roteiroService;
	private readonly ISalaService _salaService;

	private readonly Account? _account;

	public CalendarioService(
		DataContext db,
		IMapper mapper,
		IRoteiroService roteiroService,
		ISalaService salaService,
		IHttpContextAccessor httpContextAccessor
	)
	{
		_db = db;
		_mapper = mapper;
		_roteiroService = roteiroService;
		_salaService = salaService;
		_account = (Account?)httpContextAccessor.HttpContext?.Items["Account"];
	}

	public List<CalendarioEventoList> GetCalendario(CalendarioRequest request)
	{

		DateTime now = TimeFunctions.HoraAtualBR();

		request.IntervaloDe ??= GetThisWeeksMonday(now); // Se não passar data inicio, considera a segunda-feira da semana atual
		request.IntervaloAte ??= GetThisWeeksSaturday((DateTime)request.IntervaloDe); // Se não passar data fim, considera o sábado da semana da data inicio

		if (request.IntervaloAte < request.IntervaloDe)
		{
			throw new Exception("Final do intervalo não pode ser antes do seu próprio início");
		}


		var eventosQueryable = _db.CalendarioEventoLists
			.Where(e => e.Data.Date >= request.IntervaloDe.Value.Date && e.Data.Date <= request.IntervaloAte.Value.Date)
			.AsQueryable();

		var alunosQueryable = _db.AlunoLists
			.Where(x => x.Active == true)
			.AsQueryable();

		var professoresQueryable = _db.ProfessorLists
			.Where(x => x.Active == true)
			.AsQueryable();

		var turmasQueryable = _db.TurmaLists
			.Where(x => x.Active == true)
			.AsQueryable();

		if (request.Perfil_Cognitivo_Id.HasValue)
		{
			// Eventos que contem o perfil cognitivo 
			var eventosContemPerfilCognitivo = _db.Evento_Aula_PerfilCognitivo_Rels.Where(x => x.PerfilCognitivo_Id == request.Perfil_Cognitivo_Id);
			var turmasContemPerfilCognitivo = _db.Turma_PerfilCognitivo_Rels.Where(x => x.PerfilCognitivo_Id == request.Perfil_Cognitivo_Id);

			eventosQueryable = eventosQueryable.Where(e => eventosContemPerfilCognitivo.Any(x => x.Evento_Aula_Id == e.Id));
			turmasQueryable = turmasQueryable.Where(t => turmasContemPerfilCognitivo.Any(x => x.Turma_Id == t.Id));
		}

		if (request.Turma_Id.HasValue)
		{
			eventosQueryable = eventosQueryable.Where(e => e.Turma_Id == request.Turma_Id);
			turmasQueryable = turmasQueryable.Where(t => t.Id == request.Turma_Id);
		}

		if (request.Professor_Id.HasValue)
		{
			// Busca o professor em evento.Professor_Id e evento.Evento_Participacao_Professor
			var eventosContemProfessor = _db.Evento_Participacao_Professors.Where(x => x.Professor_Id == request.Professor_Id.Value);
			eventosQueryable = eventosQueryable.Where(e => e.Professor_Id != null && (e.Professor_Id == request.Professor_Id || eventosContemProfessor.Any(x => x.Evento_Id == e.Id)));
			turmasQueryable = turmasQueryable.Where(t => t.Professor_Id == request.Professor_Id);
			professoresQueryable = professoresQueryable.Where(x => x.Id == request.Professor_Id.Value);
		}

		if (request.Aluno_Id.HasValue)
		{
			var aluno = _db.Alunos.FirstOrDefault(a => a.Id == request.Aluno_Id);

			if (aluno is not null)
			{
				turmasQueryable = turmasQueryable.Where(t => t.Id == aluno.Turma_Id);
				// Busca o aluno em evento.Evento_Participacao_Aluno fora do where de eventos, para fazer menos filtros
				var eventosContemAlunos = _db.Evento_Participacao_Alunos.Where(x => x.Aluno_Id == request.Aluno_Id.Value);
				eventosQueryable = eventosQueryable.Where(e => eventosContemAlunos.Any(p => p.Evento_Id == e.Id));
				
			}
		}

		var turmasIds = turmasQueryable.Select(x => x.Id);
		var alunosIds = alunosQueryable.Select(x => x.Id);


		var turmas = turmasQueryable
			.ToList();
		
		var professores = professoresQueryable
			.ToList();

		var alunos = alunosQueryable
			.ToList();

		var vigencias = _db.Aluno_Turma_Vigencia
			.Where(x => turmasIds.Contains(x.Turma_Id) 
							&& alunosIds.Contains(x.Aluno_Id) 
							&& x.DataInicioVigencia.Date <= request.IntervaloDe.Value.Date
							&& (!x.DataFimVigencia.HasValue || x.DataFimVigencia.Value.Date >= request.IntervaloAte.Value.Date))
			.ToList();

		var perfisCognitivosTurmas = _db.Turma_PerfilCognitivo_Rels
			.Where(x => turmasIds.Contains(x.Turma_Id))
			.Include(x => x.PerfilCognitivo)
			.ToList();

		var roteiros = _roteiroService.GetAll(request.IntervaloDe.Value.Year);
		if (request.IntervaloDe.Value.Year != request.IntervaloAte.Value.Year)
		{
			var roteirosAte = _roteiroService.GetAll(request.IntervaloAte.Value.Year);
			roteiros.AddRange(roteirosAte);
		}

		var calendarioResponse = eventosQueryable.ToList();

		PopulateCalendarioEvents(calendarioResponse);

		DateTime data = request.IntervaloDe.Value;


		while(data <= request.IntervaloAte.Value)
		{

			var diaSemana = data.DayOfWeek;

			var roteiroDoDia = roteiros.FirstOrDefault(x => data.Date >= x.DataInicio.Date && data.Date <= x.DataFim.Date);

			//
			// Adiciona Oficina - Se a já existe uma oficina agendada para segunda-feira, não vai adicionar
			//
			#region Adiciona Oficina
			
			if (diaSemana == DayOfWeek.Monday)
			{
				CalendarioEventoList? eventoOficina = calendarioResponse.FirstOrDefault(x => x.Data.Date == data.Date && x.Evento_Tipo_Id == (int)EventoTipo.Oficina);

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

						Data = new DateTime(data.Year, data.Month, data.Day, 10, 0, 0),
						Finalizado = false,
						Sala_Id = null,
						Sala = "Sala Indefinida"
					};

					calendarioResponse.Add(pseudoOficina);
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
				CalendarioEventoList? eventoReuniao = calendarioResponse.FirstOrDefault(x => x.Data.Date == data.Date && x.Evento_Tipo_Id == (int)EventoTipo.Reuniao);

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

					calendarioResponse.Add(pseudoReuniao);
				}
			}

			#endregion

			var vigenciasDoDia = vigencias.Where(x => data.Date >= x.DataInicioVigencia.Date
											&& (!x.DataFimVigencia.HasValue || data.Date <= x.DataFimVigencia.Value.Date))
											.ToList();

			var	turmasDoDia = turmas.Where(x => x.DiaSemana == (int)data.DayOfWeek)
								.ToList();


			foreach (TurmaList turma in turmasDoDia)
			{
				var vigenciasDaTurma = vigenciasDoDia.Where(x => x.Turma_Id == turma.Id).ToList();
				var alunosDoDiaId = vigenciasDaTurma.Select(x => x.Aluno_Id).ToList();
				var alunosTurma = alunos.Where(x => alunosDoDiaId.Contains(x.Id)).ToList();

				DateTime dataTurma = new DateTime(data.Year, data.Month, data.Day, turma.Horario!.Value.Hours, turma.Horario!.Value.Minutes, 0);

				int alunosAtivosInTurma = vigenciasDaTurma.Count;

				CalendarioEventoList pseudoAula = new()
				{
					Id = -1,
					Evento_Tipo_Id = (int)EventoTipo.Aula,
					Evento_Tipo = "Pseudo-Aula",

					Descricao = turma.Nome, // Pseudo aulas ganham o nome da turma
					DuracaoMinutos = 120, // As pseudo aulas são de uma turma e duram 2h por padrão

					Roteiro_Id = roteiroDoDia?.Id,
					Semana = roteiroDoDia?.Semana,
					Tema = roteiroDoDia?.Tema,

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

					Alunos = _mapper.Map<List<CalendarioAlunoList>>(alunos)
											.OrderBy(a => a.Aluno)
											.ToList(),
				};

				var perfilEvento = perfisCognitivosTurmas
					.Where(x => x.Turma_Id == turma.Id)
					.Select(p => p.PerfilCognitivo)
					.ToList();

				pseudoAula.PerfilCognitivo = _mapper.Map<List<PerfilCognitivoModel>>(perfilEvento);

				var professor = professores.FirstOrDefault(x => x.Id == turma.Professor_Id);

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
								Telefone = professor.Telefone ?? "",
								ExpedienteInicio = professor.ExpedienteInicio,
								ExpedienteFim = professor.ExpedienteFim,
							}
					};


				var pseudoParticipacoes = alunosTurma
					.OrderBy(x => x.Nome)
					.Select(x => new CalendarioAlunoList()
					{
						Id = -1,
						Evento_Id = -1,
						Aluno_Id = x.Id,
						Checklist = x.Checklist,
						Checklist_Id = x.Checklist_Id,
						Aluno = x.Nome,
						DataNascimento = x.DataNascimento,
						Celular = x.Celular,
						Aluno_Foto = null,
						Turma_Id = x.Turma_Id,
						Turma = x.Turma,
						//UltimaTrocaTurma = x.UltimaTrocaTurma,
						//DataInicioVigencia = x.DataInicioVigencia,
						//DataFimVigencia = x.DataFimVigencia,
						PrimeiraAula_Id = x.PrimeiraAula_Id,
						AulaZero_Id = x.AulaZero_Id,
						RestricaoMobilidade = x.RestricaoMobilidade,
						ReposicaoDe_Evento_Id = null,
						ReposicaoPara_Evento_Id = null,
						Presente = null,
						Apostila_Kit_Id = x.Apostila_Kit_Id,
						Kit = x.Kit,
						Apostila_Abaco = x.Apostila_Abaco,
						Apostila_AH = x.Apostila_AH,
						Apostila_Abaco_Id = x.Apostila_Abaco_Id,
						Apostila_AH_Id = x.Apostila_AH_Id,
						NumeroPaginaAbaco = x.NumeroPaginaAbaco,
						NumeroPaginaAH = x.NumeroPaginaAH,
						Observacao = x.Observacao,
						Deactivated = x.Deactivated,
						AlunoContactado = null,
						ContatoObservacao = null,
						StatusContato_Id = null,
						PerfilCognitivo_Id = x.PerfilCognitivo_Id,
						PerfilCognitivo = x.PerfilCognitivo,
					})
					.ToList();

				pseudoAula.Alunos = pseudoParticipacoes;


				calendarioResponse.Add(pseudoAula);
			}

			data = data.AddDays(1);
		}

		return calendarioResponse;
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

	private List<CalendarioEventoList> PopulateCalendarioEvents(List<CalendarioEventoList> events)
	{
		var eventoIds = events.Select(e => e.Id);

		var eventoAulaIds = events
			.Where(e => e.Evento_Tipo_Id == (int)EventoTipo.Aula || e.Evento_Tipo_Id == (int)EventoTipo.AulaExtra)
			.Select(e => e.Id);

		// Fazendo o possível pra otimizar, mas CalendarioAlunoList é uma view, então não lida muito bem com chaves
		var query = from a in _db.CalendarioAlunoLists
					join p in _db.Evento_Participacao_Alunos on a.Id equals p.Id
					where eventoIds.Contains(p.Evento_Id)
					orderby a.Aluno
					select a;

		var allAlunos = query.ToList();

		List<CalendarioProfessorList> allProfessores = _db.CalendarioProfessorLists
			.Where(p => eventoIds.Contains(p.Evento_Id))
			.ToList();

		List<Evento_Aula_PerfilCognitivo_Rel> allRels = _db.Evento_Aula_PerfilCognitivo_Rels
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
			.ToDictionary(
				g => g.Key,
				g => g.Select(r => _mapper.Map<PerfilCognitivoModel>(r.PerfilCognitivo)).ToList()
			);

		foreach (var calendarioEvent in events)
		{
			alunosDictionary.TryGetValue(calendarioEvent.Id, out var alunosInEvent);
			calendarioEvent.Alunos = alunosInEvent ?? new List<CalendarioAlunoList>();

			professorDictionary.TryGetValue(calendarioEvent.Id, out var professoresInEvent);
			calendarioEvent.Professores = professoresInEvent ?? new List<CalendarioProfessorList>();

			if (perfisDictionary.TryGetValue(calendarioEvent.Id, out var aulaPerfisInEvent))
			{
				calendarioEvent.PerfilCognitivo = aulaPerfisInEvent;
			}
			else
			{
				calendarioEvent.PerfilCognitivo = new List<PerfilCognitivoModel>();
			}
		}

		return events;
	}

}
