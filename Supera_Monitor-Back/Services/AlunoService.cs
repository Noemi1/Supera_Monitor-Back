using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.CRM4U;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Aluno;

namespace Supera_Monitor_Back.Services;

public interface IAlunoService
{

	List<AlunoList> GetAll();
	List<AlunoList> GetAlunosAulaZeroDropdown();
	List<AlunoList> GetAlunosPrimeiraAulaDropdown();
	List<AlunoList> GetAlunosReposicaoDeDropdown(int evento_Id);
	List<AlunoList> GetAlunosReposicaoParaDropdown(int evento_Id);

	AlunoResponse Get(int alunoId);
	List<AlunoHistoricoList> GetHistoricoById(int alunoId);
	List<AlunoVigenciaList> GetVigenciaById(int alunoId);
	List<ApostilaList> GetApostilasByAluno(int alunoId);
	ResponseModel GetProfileImage(int alunoId);
	ResponseModel Insert(CreateAlunoRequest model);
	ResponseModel Update(UpdateAlunoRequest model);
	ResponseModel ToggleDeactivate(int alunoId);
}

public class AlunoService : IAlunoService
{
	private readonly DataContext _db;
	private readonly _CRM4UContext _dbCRM;
	private readonly IMapper _mapper;
	private readonly IChecklistService _checklistService;

	private readonly Account? _account;

	public AlunoService(
		DataContext db,
		_CRM4UContext dbCRM,
		IMapper mapper,
		IHttpContextAccessor httpContextAccessor,
		IChecklistService checklistService
		)
	{
		_db = db;
		_dbCRM = dbCRM;
		_mapper = mapper;
		_checklistService = checklistService;
		_account = (Account?)httpContextAccessor?.HttpContext?.Items["Account"];
	}

	public List<AlunoList> GetAll()
	{
		List<AlunoList> alunos = _db.AlunoList
			.OrderBy(a => a.Nome)
			.ToList();

		return alunos;
	}

	public List<AlunoList> GetAlunosReposicaoDeDropdown(int evento_Id)
	{
		//var participacao = _db.CalendarioAlunoList
		//	.Where(x => x.Evento_Id == reposicaoDe_Evento_Id
		//				&& x.ReposicaoPara_Evento_Id != null
		//				&& (x.ReposicaoDe_Evento_Id == null || x.Presente != false))
		//	.ToList();

		//var Aluno_Id = participacao.Select(x => x.Aluno_Id).ToList();

		//var alunos = _db.AlunoList
		//	.Where(x => x.Active && Aluno_Id.Contains(x.Id ) )
		//	.ToList();

		var alunos = _db.AlunoList
			.Where(aluno => aluno.Active &&
				!_db.CalendarioAlunoList.Any(participacao =>
					participacao.Evento_Id == evento_Id
					&& participacao.ReposicaoPara_Evento_Id != null
					&& (participacao.ReposicaoDe_Evento_Id == null || participacao.Presente != false)
					&& participacao.Aluno_Id == aluno.Id
				)
			)
			.ToList();
		return alunos;
	}
	
	public List<AlunoList> GetAlunosReposicaoParaDropdown(int evento_Id)
	{
		//var participacao = _db.CalendarioAlunoList
		//	.Where(x => x.Evento_Id == evento_Id);

		//var Aluno_Id = participacao.Select(x => x.Aluno_Id).ToList();

		//var evento = _db.Evento
		//	.Include(x => x.Evento_Aula.Evento_Aula_PerfilCognitivo_Rel)
		//	.ThenInclude(x => x.PerfilCognitivo)
		//	.FirstOrDefault(x => x.Id == evento_Id);

		//var perfis = evento.Evento_Aula.Evento_Aula_PerfilCognitivo_Rel.Select(x => x.PerfilCognitivo_Id).ToList();	

		//var alunos = _db.AlunoList
		//	.Where(aluno => aluno.Active
		//		&& Aluno_Id.Contains(aluno.Id) == false
		//		&& (aluno.PerfilCognitivo_Id == null || perfis.Contains(aluno.PerfilCognitivo_Id.Value) )
		//		&& (aluno.RestricaoMobilidade == false || evento.Sala.Andar == (int)SalaAndar.Terreo)
		//		)
		//	.ToList();

		var eventoInfo = _db.Evento
		.Where(e => e.Id == evento_Id)
		.Select(e => new
		{
			SalaAndar = e.Sala.Andar,
			Perfis = e.Evento_Aula
				.Evento_Aula_PerfilCognitivo_Rel
				.Select(p => p.PerfilCognitivo_Id)
		})
		.FirstOrDefault();

		if (eventoInfo == null)
			return this.GetAll();

		var alunos = _db.AlunoList
			.Where(aluno =>
				aluno.Active

				// Aluno ainda não está no evento
				&& !_db.CalendarioAlunoList.Any(c =>
					c.Evento_Id == evento_Id &&
					c.Aluno_Id == aluno.Id
				)

				// Perfil cognitivo compatível
				&& (
					aluno.PerfilCognitivo_Id == null ||
					eventoInfo.Perfis.Contains(aluno.PerfilCognitivo_Id.Value)
				)

				// Restrição de mobilidade
				&& (
					!aluno.RestricaoMobilidade ||
					eventoInfo.SalaAndar == (int)SalaAndar.Terreo
				)
			)
			.AsNoTracking()
			.ToList();

		return alunos;
	}

	public List<AlunoList> GetAlunosAulaZeroDropdown()
	{
		var alunosQueryable = _db.AlunoList.Where(aluno => aluno.AulaZero_Id == null
				|| _db.Evento_Participacao_Aluno.Any(x => x.Evento_Id == aluno.AulaZero_Id && x.Presente != true));

		List<AlunoList> alunos = alunosQueryable
			.OrderBy(a => a.Nome)
			.ToList();

		return alunos;
	}
	
	public List<AlunoList> GetAlunosPrimeiraAulaDropdown()
	{
		var alunosQueryable = _db.AlunoList.Where(aluno => aluno.PrimeiraAula_Id == null
				|| _db.Evento_Participacao_Aluno.Any(x => x.Evento_Id == aluno.PrimeiraAula_Id && x.Presente != true));

		List<AlunoList> alunos = alunosQueryable
			.OrderBy(a => a.Nome)
			.ToList();

		return alunos;
	}

	public AlunoResponse Get(int alunoId)
	{
		AlunoList? aluno = _db.AlunoList
			.AsNoTracking()
			.FirstOrDefault(a => a.Id == alunoId);

		if (aluno is null)
		{
			throw new Exception("Aluno não encontrado");
		}

		AlunoResponse model = _mapper.Map<AlunoResponse>(aluno);

		//model.AlunoChecklist = _db.AlunoChecklistViews
		//	.Where(a => a.Aluno_Id == model.Id)
		//	.ToList();

		model.Restricoes = _db.AlunoRestricaoLists
			.Where(ar => ar.Aluno_Id == aluno.Id)
			.ToList();

		return model;
	}

	public ResponseModel Insert(CreateAlunoRequest request)
	{

		ResponseModel response = new() { Success = false };

		try
		{
			PessoaCRM? pessoaCRM = _dbCRM.Pessoa.Find(request.Pessoa_Id);

			// Aluno só pode ser cadastrado se a pessoa existir no CRM
			//if (pessoaCRM == null)
			//{
			//	return new ResponseModel { Message = "Pessoa não encontrada" };
			//}

			//// Aluno só pode ser cadastrado se tiver status matriculado
			//if (pessoaCRM.Pessoa_Status_Id != (int)PessoaStatus.Matriculado)
			//{
			//	return new ResponseModel { Message = "Aluno não está matriculado" };
			//}

			//// Aluno só pode ser cadastrado se tiver Unidade_Id = 1 (Supera Brigadeiro)
			//if (pessoaCRM.Unidade_Id != 1)
			//{
			//	return new ResponseModel { Message = "Pessoa não pertence a uma unidade cadastrada" };
			//}

			//// Só pode ser cadastrado um aluno por pessoa
			//bool alunoAlreadyRegistered = _db.Pessoas.Any(a => a.PessoaCRM_Id == request.Pessoa_Id);
			//if (alunoAlreadyRegistered)
			//{
			//	return new ResponseModel { Message = "Aluno já matriculado." };
			//}

			if(!_db.AspNetUsers.Any(x => x.Id == request.AspNetUsers_Created_Id))
			{
				var aspNetUser = _dbCRM.AspNetUsers.FirstOrDefault(x => x.Id == request.AspNetUsers_Created_Id);
				_db.AspNetUsers.Add(new AspNetUser
				{
					Id = aspNetUser.Id,
					Name = aspNetUser.Name,
					UserName = aspNetUser.UserName,
					Email = aspNetUser.Email,
					PhoneNumber = aspNetUser.PhoneNumber,
				});
				_db.SaveChanges();
			}

			// Validations passed
			Pessoa pessoa = new Pessoa()
			{
				PessoaCRM_Id = pessoaCRM.Id,
				Nome = pessoaCRM.Nome,
				Email = pessoaCRM.Email,
				Endereco = pessoaCRM.Endereco,
				Observacao = pessoaCRM.Observacao,
				Telefone = pessoaCRM.Telefone,
				Celular = pessoaCRM.Celular,
				DataEntrada = pessoaCRM.DataEntrada,
				Pessoa_FaixaEtaria_Id = pessoaCRM.Pessoa_FaixaEtaria_Id,
				Pessoa_Origem_Id = pessoaCRM.Pessoa_Origem_Id,
				Pessoa_Status_Id = pessoaCRM.Pessoa_Status_Id,
				RG = pessoaCRM.RG,
				CPF = pessoaCRM.CPF,
				aspnetusers_Id = pessoaCRM.aspnetusers_Id,
				Pessoa_Sexo_Id = pessoaCRM.Pessoa_Sexo_Id,
				DataNascimento = pessoaCRM.DataNascimento,
				DataCadastro = pessoaCRM.DataCadastro,
				Unidade_Id = pessoaCRM.Unidade_Id,
				Pessoa_Origem_Canal_Id = pessoaCRM.Pessoa_Origem_Canal_Id,
				Pessoa_Indicou_Id = pessoaCRM.Pessoa_Indicou_Id,
				LandPage_Id = pessoaCRM.LandPage_Id,
				Pessoa_Geracao_Id = pessoaCRM.Pessoa_Geracao_Id,
			};

			Aluno aluno = new Aluno()
			{
				Pessoa_Id = pessoa.Id,
				AspNetUsers_Created_Id = request.AspNetUsers_Created_Id,
				Created = TimeFunctions.HoraAtualBR(),
				LastUpdated = null,
				Deactivated = null,

				RM = Utils.GenerateRM(_db),
				LoginApp = pessoa.Email,
				SenhaApp = "Supera@123",

				Turma_Id = null,
				PerfilCognitivo_Id = null,
				AulaZero_Id = null,
				PrimeiraAula_Id = null,
				Aluno_Foto = null,
				RestricaoMobilidade = null,

				Apostila_Kit_Id = null,
				Apostila_Abaco_Id = null,
				NumeroPaginaAbaco = null,
				Apostila_AH_Id = null,
				NumeroPaginaAH = null,
			};

			aluno.Aluno_Historicos.Add(new Aluno_Historico
			{
				Aluno_Id = aluno.Id,
				Descricao = "Aluno cadastrado",
				AspNetUser_Id = request.AspNetUsers_Created_Id,
				Account_Id = _account?.Id,
				Data = TimeFunctions.HoraAtualBR(),
			});
			pessoa.Alunos = new List<Aluno>() { aluno };

			_db.Add(pessoa);
			_db.SaveChanges();

			ResponseModel populateChecklistResponse = _checklistService.PopulateAlunoChecklist(aluno.Id);

			if (!populateChecklistResponse.Success)
			{
				return populateChecklistResponse;
			}

			response.Message = "Aluno cadastrado com sucesso";
			response.Object = _db.AlunoList.AsNoTracking().FirstOrDefault(a => a.Id == aluno.Id);
			response.Success = true;
		}
		catch (Exception ex)
		{
			response.Message = $"Falha ao registrar aluno: {ex}";
		}

		return response;
	}

	public ResponseModel Update(UpdateAlunoRequest model)
	{
		ResponseModel response = new() { Success = false };

		try
		{
			Aluno? aluno = _db.Aluno
				.Include(a => a.Pessoa)
				.Include(a => a.Aluno_Turma_Vigencia)
				.FirstOrDefault(a => a.Id == model.Id);

			if (aluno == null)
			{
				return new ResponseModel { Message = "Aluno não encontrado" };
			}

			if (aluno.Pessoa is null)
			{
				return new ResponseModel { Message = "Aluno não encontrado" };
			}

			AlunoList? oldObject = _db.AlunoList.AsNoTracking().FirstOrDefault(a => a.Id == model.Id);

			if (oldObject is null)
			{
				return new ResponseModel { Message = "Aluno não encontrado" };
			}

			// Não deve ser possível atualizar um aluno com um perfil cognitivo que não existe
			var updatedPerfilCognitivo = _db.PerfilCognitivos.FirstOrDefault(p => p.Id == model.PerfilCognitivo_Id);

			if (model.PerfilCognitivo_Id.HasValue && updatedPerfilCognitivo is null)
			{
				return new ResponseModel { Message = "Não é possível atualizar um aluno com um perfil cognitivo que não existe" };
			}

			// Aluno só pode ser operado em atualizações ou troca de turma se for uma turma válida
			Turma? turmaDestino = _db.Turmas
				.Include(t => t.Alunos)
				.FirstOrDefault(t => t.Id == model.Turma_Id);

			if (turmaDestino is null && model.Turma_Id.HasValue)
			{
				return new ResponseModel { Message = "Turma não encontrada" };
			}

			bool isChangingPerfilCognitivo = aluno.PerfilCognitivo_Id != model.PerfilCognitivo_Id;

			if (isChangingPerfilCognitivo)
			{
				var currentPerfilCognitivo = _db.PerfilCognitivos.Find(aluno.PerfilCognitivo_Id);

				_db.Aluno_Historico.Add(new Aluno_Historico
				{
					Aluno_Id = aluno.Id,
					Descricao = $"Perfil cognitivo do aluno foi atualizado de '{currentPerfilCognitivo?.Descricao}' para '{updatedPerfilCognitivo?.Descricao}'.",
					Account_Id = _account!.Id,
					Data = TimeFunctions.HoraAtualBR(),
				});
			}

			if (turmaDestino is not null && aluno.Turma_Id != turmaDestino.Id)
			{
				int countAlunosInTurma = _db.Aluno.Count(a => a.Turma_Id == turmaDestino.Id && a.Deactivated == null);

				if (countAlunosInTurma >= turmaDestino.CapacidadeMaximaAlunos)
				{
					return new ResponseModel { Message = "Turma destino está em sua capacidade máxima" };
				}
			}

			// O aluno só pode receber um kit que esteja cadastrado ou nulo
			if (model.Apostila_Kit_Id is not null && model.Apostila_Kit_Id.HasValue)
			{
				bool apostilaKitExists = _db.Apostila_Kit.Any(k => k.Id == model.Apostila_Kit_Id);

				if (!apostilaKitExists)
				{
					return new ResponseModel { Message = "Não é possível atualizar um aluno com um kit que não existe" };
				}
			}

			if (model.Pessoa_Sexo_Id.HasValue)
			{
				bool pessoaSexoExists = _db.Pessoa_Sexos.Any(s => s.Id == model.Pessoa_Sexo_Id);

				if (pessoaSexoExists == false)
				{
					return new ResponseModel { Message = "Campo 'Pessoa_Sexo_Id' é inválido" };
				}
			}

			// Garantir que RM é unico pra cada aluno
			bool rmIsAlreadyTaken = _db.Aluno.Any(a => a.RM == model.RM && a.Id != model.Id);

			if (rmIsAlreadyTaken)
			{
				return new ResponseModel { Message = "RM já existe" };
			}

			// Validations passed

			bool trocandoDeTurma = turmaDestino is not null && aluno.Turma_Id != turmaDestino.Id;
			bool removidoDaTurma = aluno.Turma_Id != null && turmaDestino is null;

			// Atualizando dados de Aluno
			aluno.RM = model.RM;
			aluno.LoginApp = model.LoginApp ?? aluno.LoginApp;
			aluno.SenhaApp = model.SenhaApp ?? aluno.SenhaApp;
			aluno.PerfilCognitivo_Id = model.PerfilCognitivo_Id;
			aluno.PrimeiraAula_Id = model.PrimeiraAula_Id;
			aluno.AulaZero_Id = model.AulaZero_Id;

			aluno.Turma_Id = model.Turma_Id;
			aluno.Aluno_Foto = model.Aluno_Foto;
			aluno.Apostila_Kit_Id = model.Apostila_Kit_Id;
			aluno.RestricaoMobilidade = model.RestricaoMobilidade;

			// Atualizando dados de Pessoa
			aluno.Pessoa.Nome = model.Nome ?? aluno.Pessoa.Nome;
			aluno.Pessoa.DataNascimento = model.DataNascimento ?? aluno.Pessoa.DataNascimento;
			aluno.Pessoa.Email = model.Email ?? aluno.Pessoa.Email;
			aluno.Pessoa.Endereco = model.Endereco ?? aluno.Pessoa.Endereco;
			aluno.Pessoa.Observacao = model.Observacao ?? aluno.Pessoa.Observacao;
			aluno.Pessoa.Telefone = model.Telefone ?? aluno.Pessoa.Telefone;
			aluno.Pessoa.Celular = model.Celular ?? aluno.Pessoa.Celular;
			aluno.Pessoa.Pessoa_Sexo_Id = model.Pessoa_Sexo_Id ?? aluno.Pessoa.Pessoa_Sexo_Id;

			aluno.LastUpdated = TimeFunctions.HoraAtualBR();


			_db.Aluno_Historico.Add(new Aluno_Historico
			{
				Aluno_Id = aluno.Id,
				Descricao = $"Dados do aluno foram atualizados.",
				Account_Id = _account!.Id,
				Data = TimeFunctions.HoraAtualBR(),
			});

			_db.Update(aluno);
			_db.SaveChanges();

			/*
             * Se o aluno trocou de turma:
             * 1. Remover seu registro nas próximas aulas da turma original
             * 2. Adicionar seu registro nas próximas aulas da turma destino
             * 3. Criar uma entidade em Aluno_Historico como 'log' da mudança
            */

			if (trocandoDeTurma)
			{

				DateTime data = TimeFunctions.HoraAtualBR();
				Aluno_Turma_Vigencia? ultimaVigencia = _db.Aluno_Turma_Vigencia.FirstOrDefault(x => x.Turma_Id == oldObject.Turma_Id
																					&& x.Aluno_Id == model.Id
																					&& !x.DataFimVigencia.HasValue);
				if (ultimaVigencia is not null)
				{
					ultimaVigencia.DataFimVigencia = data;
					_db.Aluno_Turma_Vigencia.Update(ultimaVigencia);
				}


				_db.Aluno_Turma_Vigencia.Add(new Aluno_Turma_Vigencia
				{
					Account_Id = _account.Id,
					Aluno_Id = aluno.Id,
					Turma_Id = aluno.Turma_Id.Value,
					DataInicioVigencia = data,
				});

				List<Evento> eventosTurmaOriginal = _db.Evento
					.Include(e => e.Evento_Aula)
					.Where(e =>
						e.Evento_Aula != null
						&& e.Data >= TimeFunctions.HoraAtualBR()
						&& e.Evento_Aula.Turma_Id == oldObject.Turma_Id)
					.ToList();

				// Para cada aula da turma original, remover os registros do aluno sendo trocado, se existirem
				foreach (Evento evento in eventosTurmaOriginal)
				{
					Evento_Participacao_Aluno? participacaoAluno = _db.Evento_Participacao_Aluno
						.FirstOrDefault(p =>
							p.Evento_Id == evento.Id &&
							p.Aluno_Id == aluno.Id);

					if (participacaoAluno is null)
					{
						continue;
					}

					_db.Evento_Participacao_Aluno.Remove(participacaoAluno);
				}

				List<Evento> eventosTurmaDestino = _db.Evento
					.Include(e => e.Evento_Aula)
					.Include(e => e.Evento_Participacao_Aluno)
					.Where(e =>
						e.Evento_Aula != null
						&& e.Data >= TimeFunctions.HoraAtualBR()
						&& e.Evento_Aula.Turma_Id == aluno.Turma_Id)
					.ToList();

				// Inserir novos registros deste aluno nas aulas futuras da turma destino
				foreach (Evento evento in eventosTurmaDestino)
				{
					Evento_Participacao_Aluno newParticipacao = new()
					{
						Presente = null,
						Aluno_Id = aluno.Id,
						Evento_Id = evento.Id,
					};

					// Aula não deve registrar aluno se estiver em sua capacidade máxima e nesse caso, -> considera os alunos de reposição <-
					int amountOfAlunosInAula = evento.Evento_Participacao_Aluno.Count(p => p.Deactivated == null);

					if (amountOfAlunosInAula >= evento.CapacidadeMaximaAlunos)
					{
						continue;
					}

					_db.Evento_Participacao_Aluno.Add(newParticipacao);
				}

				_db.Aluno_Historico.Add(new Aluno_Historico
				{
					Aluno_Id = aluno.Id,
					Descricao = $"Aluno foi transferido da turma: '{oldObject?.Turma}' para a turma : '{aluno.Turma.Nome}'",
					Account_Id = _account!.Id,
					Data = TimeFunctions.HoraAtualBR(),
				});
			}
			else if (removidoDaTurma)
			{
				DateTime data = TimeFunctions.HoraAtualBR();
				Aluno_Turma_Vigencia ultimaVigencia = _db.Aluno_Turma_Vigencia.First(x => x.Turma_Id == oldObject.Turma_Id
																					&& x.Aluno_Id == model.Id
																					&& !x.DataFimVigencia.HasValue);

				if (ultimaVigencia is not null)
				{
					ultimaVigencia.DataFimVigencia = data;
					_db.Aluno_Turma_Vigencia.Update(ultimaVigencia);
				}

				_db.Aluno_Historico.Add(new Aluno_Historico
				{
					Aluno_Id = aluno.Id,
					Descricao = $"Aluno foi removido da turma: '{oldObject?.Turma}'.",
					Account_Id = _account!.Id,
					Data = TimeFunctions.HoraAtualBR(),
				});
			}

			_db.SaveChanges();

			response.Message = "Aluno atualizado com sucesso";
			response.OldObject = oldObject;
			response.Object = _db.AlunoList.AsNoTracking().FirstOrDefault(aluno => aluno.Id == model.Id);
			response.Success = true;

		}
		catch (Exception ex)
		{
			response.Message = $"Falha ao atualizar aluno: {ex}";
		}

		return response;
	}

	public ResponseModel ToggleDeactivate(int alunoId)
	{
		ResponseModel response = new() { Success = false };

		try
		{
			Aluno? aluno = _db.Aluno.Find(alunoId);

			if (aluno == null)
			{
				return new ResponseModel { Message = "Aluno não encontrado." };
			}

			if (_account == null)
			{
				return new ResponseModel { Message = "Não foi possível completar a ação. Autenticação do autor não encontrada." };
			}

			// Validations passed

			bool isAlunoActive = aluno.Active;


			// Toggle Status Aluno
			aluno.Turma_Id = null;

			if (aluno.Active)
			{
				DateTime data = TimeFunctions.HoraAtualBR();

				Aluno_Turma_Vigencia ultimaVigencia = _db.Aluno_Turma_Vigencia.First(x => x.Aluno_Id == aluno.Id && !x.DataFimVigencia.HasValue);
				ultimaVigencia.DataFimVigencia = data;
				_db.Aluno_Turma_Vigencia.Update(ultimaVigencia);

				aluno.Deactivated = data;

			}
			else
			{
				aluno.Deactivated = null;
			}


			_db.Aluno.Update(aluno);

			_db.Aluno_Historico.Add(new Aluno_Historico
			{
				Aluno_Id = aluno.Id,
				Descricao = $"Aluno {(aluno.Deactivated.HasValue ? "Reativado" : "Desativado")}",
				Account_Id = _account.Id,
				Data = TimeFunctions.HoraAtualBR(),
			});

			_db.SaveChanges();

			string toggleResult = aluno.Deactivated == null ? "reativado" : "desativado";

			response.Success = true;
			response.Message = $"Aluno foi {toggleResult} com sucesso";
			response.Object = _db.AlunoList.AsNoTracking().FirstOrDefault(a => a.Id == aluno.Id);
		}
		catch (Exception ex)
		{
			response.Message = $"Falha ao ativar/desativar aluno: {ex}";
		}

		return response;
	}

	public ResponseModel GetProfileImage(int alunoId)
	{
		ResponseModel response = new() { Success = false };

		try
		{
			Aluno? aluno = _db.Aluno.Find(alunoId);

			if (aluno is null)
			{
				return new ResponseModel { Message = "Aluno não encontrado" };
			}

			response.Success = true;
			response.Message = "Imagem de perfil encontrada";
			response.Object = aluno.Aluno_Foto;
		}
		catch (Exception ex)
		{
			response.Message = $"Falha ao resgatar imagem de perfil do aluno: {ex}";
		}

		return response;
	}

	public List<ApostilaList> GetApostilasByAluno(int alunoId)
	{
		Aluno? aluno = _db.Aluno.Find(alunoId);

		if (aluno is null)
		{
			throw new Exception("Aluno não encontrado");
		}

		List<ApostilaList> apostilas = _db.ApostilaLists
			.Where(a => a.Apostila_Kit_Id == aluno.Apostila_Kit_Id)
			.ToList();

		return apostilas;
	}

	public List<AlunoHistoricoList> GetHistoricoById(int alunoId)
	{
		List<AlunoHistoricoList> list = _db.AlunoHistoricoList
			.Where(h => h.Aluno_Id == alunoId)
			.ToList();

		return list;

	}

	public List<AlunoVigenciaList> GetVigenciaById(int alunoId)
	{
		List<AlunoVigenciaList> list = _db.AlunoVigenciaList
			.Where(h => h.Aluno_Id == alunoId)
			.ToList();

		return list;

	}

}
