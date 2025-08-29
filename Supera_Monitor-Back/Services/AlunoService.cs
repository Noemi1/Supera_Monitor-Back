using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.CRM4U;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Aluno;
using Supera_Monitor_Back.Services.Email;

namespace Supera_Monitor_Back.Services;

public interface IAlunoService {
    AlunoListWithChecklist Get(int alunoId);
    List<AlunoList> GetAll();
    List<AlunoHistoricoList> GetHistoricoById(int alunoId);
    List<AlunoListWithChecklist> GetAllWithChecklist(AlunoRequest request);
    List<AlunoChecklistItemList> GetAlunoChecklistItemList(AlunoRequest request);
    ResponseModel GetProfileImage(int alunoId);
    ResponseModel GetSummaryByAluno(int alunoId);
    List<ApostilaList> GetApostilasByAluno(int alunoId);
    ResponseModel Insert(CreateAlunoRequest model);
    ResponseModel PrimeiraAula(PrimeiraAulaRequest model);
    ResponseModel Reposicao(ReposicaoRequest model);
    ResponseModel Update(UpdateAlunoRequest model);
    ResponseModel ToggleDeactivate(int alunoId);
}

public class AlunoService : IAlunoService {
    private readonly DataContext _db;
    private readonly CRM4UContext _dbCRM;
    private readonly IMapper _mapper;
    private readonly IChecklistService _checklistService;

    private readonly Account? _account;

    public AlunoService(
        DataContext db,
        CRM4UContext dbCRM,
        IMapper mapper,
        IPessoaService pessoaService,
        IHttpContextAccessor httpContextAccessor,
        IChecklistService checklistService,
        IEmailService emailService
        ) {
        _db = db;
        _dbCRM = dbCRM;
        _mapper = mapper;
        _checklistService = checklistService;
        _account = (Account?)httpContextAccessor?.HttpContext?.Items["Account"];
    }

    public AlunoListWithChecklist Get(int alunoId) {
        AlunoList? aluno = _db.AlunoLists.AsNoTracking().SingleOrDefault(a => a.Id == alunoId);

        if (aluno is null) {
            throw new Exception("Aluno não encontrado");
        }

        AlunoListWithChecklist alunoListWithChecklist = _mapper.Map<AlunoListWithChecklist>(aluno);

        alunoListWithChecklist.AlunoChecklist = _db.AlunoChecklistViews
            .Where(a => a.Aluno_Id == alunoListWithChecklist.Id)
            .ToList();

        alunoListWithChecklist.Restricoes = _db.AlunoRestricaoLists
            .Where(ar => ar.Aluno_Id == aluno.Id)
            .ToList();

        return alunoListWithChecklist;
    }

    public List<AlunoList> GetAll() {
        List<AlunoList> alunos = _db.AlunoLists.OrderBy(a => a.Nome).ToList();

        return alunos;
    }

    public List<AlunoListWithChecklist> GetAllWithChecklist(AlunoRequest request) {
        IQueryable<AlunoList> alunosQueryable = _db.AlunoLists
          .Where(a => a.Deactivated == null)
          .AsQueryable();


        if (request.Turma_Id.HasValue) {
            alunosQueryable = alunosQueryable.Where(a => a.Turma_Id == request.Turma_Id);
        }

        if (request.Professor_Id.HasValue) {
            alunosQueryable = alunosQueryable.Where(a => a.Professor_Id == request.Professor_Id);
        }

        if (request.Aluno_Id.HasValue) {
            alunosQueryable = alunosQueryable.Where(a => a.Id == request.Aluno_Id);
        }


        List<AlunoList> alunos = alunosQueryable.OrderBy(a => a.Nome).ToList();
        List<int> alunosIds = alunos.Select(x => x.Id).ToList();

        List<AlunoListWithChecklist> alunosWithChecklist = _mapper.Map<List<AlunoListWithChecklist>>(alunos);


        List<AlunoChecklistView> listAlunoChecklistView = _db.AlunoChecklistViews
            .Where(c => alunosIds.Contains(c.Aluno_Id))
            .ToList();

        alunosWithChecklist.ForEach(aluno =>
        {
            aluno.AlunoChecklist = listAlunoChecklistView
                .Where(a => aluno.Id == a.Aluno_Id)
                .OrderBy(a => a.Checklist_Id)
                .ThenBy(a => a.Ordem)
                .ToList();
        });

        return alunosWithChecklist;
    }

    public ResponseModel Insert(CreateAlunoRequest model) {

        ResponseModel response = new() { Success = false };

        try {
            PessoaCRM? pessoaCRM = _dbCRM.Pessoa.Find(model.Pessoa_Id);

            // Aluno só pode ser cadastrado se a pessoa existir no CRM
            if (pessoaCRM == null) {
                return new ResponseModel { Message = "Pessoa não encontrada" };
            }

            // Aluno só pode ser cadastrado se tiver status matriculado
            if (pessoaCRM.Pessoa_Status_Id != (int)PessoaStatus.Matriculado) {
                return new ResponseModel { Message = "Aluno não está matriculado" };
            }

            // Aluno só pode ser cadastrado se tiver Unidade_Id = 1 (Supera Brigadeiro)
            if (pessoaCRM.Unidade_Id != 1) {
                return new ResponseModel { Message = "Pessoa não pertence a uma unidade cadastrada" };
            }


            // Só pode ser cadastrado um aluno por pessoa
            bool alunoAlreadyRegistered = _db.Pessoas.Any(a => a.PessoaCRM_Id == model.Pessoa_Id);
            if (alunoAlreadyRegistered) {
                return new ResponseModel { Message = "Aluno já matriculado." };
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

            _db.Pessoas.Add(pessoa);
            _db.SaveChanges();

            string randomRM = Utils.GenerateRM(_db);

            Aluno aluno = new()
            {
                Pessoa_Id = pessoa.Id,
                AspNetUsers_Created_Id = model.AspNetUsers_Created_Id,
                Created = TimeFunctions.HoraAtualBR(),
                LastUpdated = null,
                Deactivated = null,

                RM = randomRM,
                LoginApp = pessoa.Email ?? $"{randomRM}@supera",
                SenhaApp = "Supera@123",

                DataInicioVigencia = TimeFunctions.HoraAtualBR(),
                DataFimVigencia = null,
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

            _db.Alunos.Add(aluno);
            _db.SaveChanges();

            ResponseModel populateChecklistResponse = _checklistService.PopulateAlunoChecklist(aluno.Id);

            if (!populateChecklistResponse.Success) {
                return populateChecklistResponse;
            }

            _db.Aluno_Historicos.Add(new Aluno_Historico
            {
                Aluno_Id = aluno.Id,
                Descricao = "Aluno cadastrado",
                AspNetUser_Id = model.AspNetUsers_Created_Id,
                Account_Id = _account?.Id,
                Data = TimeFunctions.HoraAtualBR(),
            });

            _db.SaveChanges();

            response.Message = "Aluno cadastrado com sucesso";
            response.Object = _db.AlunoLists.AsNoTracking().FirstOrDefault(a => a.Id == aluno.Id);
            response.Success = true;
        }
        catch (Exception ex) {
            response.Message = $"Falha ao registrar aluno: {ex}";
        }

        return response;
    }

    public ResponseModel Update(UpdateAlunoRequest model) {
        ResponseModel response = new() { Success = false };

        try {
            Aluno? aluno = _db.Alunos
                .Include(a => a.Pessoa)
                .FirstOrDefault(a => a.Id == model.Id);

            if (aluno == null) {
                return new ResponseModel { Message = "Aluno não encontrado" };
            }

            if (aluno.Pessoa is null) {
                return new ResponseModel { Message = "Aluno não encontrado" };
            }

            AlunoList? oldObject = _db.AlunoLists.AsNoTracking().FirstOrDefault(a => a.Id == model.Id);

            if (oldObject is null) {
                return new ResponseModel { Message = "Aluno não encontrado" };
            }

            // Não deve ser possível atualizar um aluno com um perfil cognitivo que não existe
            var updatedPerfilCognitivo = _db.PerfilCognitivos.FirstOrDefault(p => p.Id == model.PerfilCognitivo_Id);

            if (model.PerfilCognitivo_Id.HasValue && updatedPerfilCognitivo is null) {
                return new ResponseModel { Message = "Não é possível atualizar um aluno com um perfil cognitivo que não existe" };
            }

            // Aluno só pode ser operado em atualizações ou troca de turma se for uma turma válida
            Turma? turmaDestino = _db.Turmas.FirstOrDefault(t => t.Id == model.Turma_Id);

            if (turmaDestino is null && model.Turma_Id.HasValue) {
                return new ResponseModel { Message = "Turma não encontrada" };
            }

            bool isChangingPerfilCognitivo = aluno.PerfilCognitivo_Id != model.PerfilCognitivo_Id;

            if (isChangingPerfilCognitivo) {
                var currentPerfilCognitivo = _db.PerfilCognitivos.Find(aluno.PerfilCognitivo_Id);

                _db.Aluno_Historicos.Add(new Aluno_Historico
                {
                    Aluno_Id = aluno.Id,
                    Descricao = $"Perfil cognitivo do aluno foi atualizado de '{currentPerfilCognitivo?.Descricao}' para '{updatedPerfilCognitivo?.Descricao}'.",
                    Account_Id = _account!.Id,
                    Data = TimeFunctions.HoraAtualBR(),
                });
            }

            // Se aluno estiver trocando de turma, deve-se garantir que a turma destino tem espaço disponível
            // Não considera reposição, pois não estamos olhando para uma aula específica
            if (turmaDestino is not null && aluno.Turma_Id != turmaDestino.Id) {
                int countAlunosInTurma = _db.Alunos.Count(a => a.Turma_Id == turmaDestino.Id && a.Deactivated == null);

                if (countAlunosInTurma >= turmaDestino.CapacidadeMaximaAlunos) {
                    return new ResponseModel { Message = "Turma destino está em sua capacidade máxima" };
                }
            }

            // O aluno só pode receber um kit que esteja cadastrado ou nulo
            if (model.Apostila_Kit_Id is not null && model.Apostila_Kit_Id.HasValue) {
                bool apostilaKitExists = _db.Apostila_Kits.Any(k => k.Id == model.Apostila_Kit_Id);

                if (!apostilaKitExists) {
                    return new ResponseModel { Message = "Não é possível atualizar um aluno com um kit que não existe" };
                }
            }

            if (model.Pessoa_Sexo_Id.HasValue) {
                bool pessoaSexoExists = _db.Pessoa_Sexos.Any(s => s.Id == model.Pessoa_Sexo_Id);

                if (pessoaSexoExists == false) {
                    return new ResponseModel { Message = "Campo 'Pessoa_Sexo_Id' é inválido" };
                }
            }

            // Garantir que RM é unico pra cada aluno
            bool rmIsAlreadyTaken = _db.Alunos.Any(a => a.RM == model.RM && a.Id != model.Id);

            if (rmIsAlreadyTaken) {
                return new ResponseModel { Message = "RM já existe" };
            }

            // Validations passed

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
            aluno.DataInicioVigencia = model.DataInicioVigencia ?? aluno.DataInicioVigencia;
            aluno.DataFimVigencia = model.DataFimVigencia ?? aluno.DataFimVigencia;
            aluno.RestricaoMobilidade = model.RestricaoMobilidade ?? aluno.RestricaoMobilidade;

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

            _db.Aluno_Historicos.Add(new Aluno_Historico
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

            bool trocandoDeTurma = turmaDestino is not null && aluno.Turma_Id != turmaDestino.Id;
            bool removidoDaTurma = aluno.Turma_Id != null && turmaDestino is null;

            if (trocandoDeTurma) {
                List<Evento> eventosTurmaOriginal = _db.Eventos
                    .Include(e => e.Evento_Aula)
                    .Where(e =>
                        e.Evento_Aula != null
                        && e.Data >= TimeFunctions.HoraAtualBR()
                        && e.Evento_Aula.Turma_Id == oldObject.Turma_Id)
                    .ToList();

                // Para cada aula da turma original, remover os registros do aluno sendo trocado, se existirem
                foreach (Evento evento in eventosTurmaOriginal) {
                    Evento_Participacao_Aluno? participacaoAluno = _db.Evento_Participacao_Alunos
                        .FirstOrDefault(p =>
                            p.Evento_Id == evento.Id &&
                            p.Aluno_Id == aluno.Id);

                    if (participacaoAluno is null) {
                        continue;
                    }

                    _db.Evento_Participacao_Alunos.Remove(participacaoAluno);
                }

                List<Evento> eventosTurmaDestino = _db.Eventos
                    .Include(e => e.Evento_Aula)
                    .Include(e => e.Evento_Participacao_Alunos)
                    .Where(e =>
                        e.Evento_Aula != null
                        && e.Data >= TimeFunctions.HoraAtualBR()
                        && e.Evento_Aula.Turma_Id == aluno.Turma_Id)
                    .ToList();

                // Inserir novos registros deste aluno nas aulas futuras da turma destino
                foreach (Evento evento in eventosTurmaDestino) {
                    Evento_Participacao_Aluno newParticipacao = new()
                    {
                        Presente = null,
                        Aluno_Id = aluno.Id,
                        Evento_Id = evento.Id,
                    };

                    // Aula não deve registrar aluno se estiver em sua capacidade máxima e nesse caso, -> considera os alunos de reposição <-
                    int amountOfAlunosInAula = evento.Evento_Participacao_Alunos.Count(p => p.Deactivated == null);

                    if (amountOfAlunosInAula >= evento.CapacidadeMaximaAlunos) {
                        continue;
                    }

                    _db.Evento_Participacao_Alunos.Add(newParticipacao);
                }

                _db.Aluno_Historicos.Add(new Aluno_Historico
                {
                    Aluno_Id = aluno.Id,
                    Descricao = $"Aluno foi transferido da turma: '{oldObject?.Turma}' para a turma : '{aluno.Turma.Nome}'",
                    Account_Id = _account!.Id,
                    Data = TimeFunctions.HoraAtualBR(),
                });
            }
            else if (removidoDaTurma) {
                _db.Aluno_Historicos.Add(new Aluno_Historico
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
            response.Object = _db.AlunoLists.AsNoTracking().FirstOrDefault(aluno => aluno.Id == model.Id);
            response.Success = true;

        }
        catch (Exception ex) {
            response.Message = $"Falha ao atualizar aluno: {ex}";
        }

        return response;
    }

    public ResponseModel ToggleDeactivate(int alunoId) {
        ResponseModel response = new() { Success = false };

        try {
            Aluno? aluno = _db.Alunos.Find(alunoId);

            if (aluno == null) {
                return new ResponseModel { Message = "Aluno não encontrado." };
            }

            if (_account == null) {
                return new ResponseModel { Message = "Não foi possível completar a ação. Autenticação do autor não encontrada." };
            }

            // Validations passed

            bool isAlunoActive = aluno.Deactivated == null;

            aluno.Deactivated = isAlunoActive ? TimeFunctions.HoraAtualBR() : null;

            _db.Alunos.Update(aluno);

            _db.Aluno_Historicos.Add(new Aluno_Historico
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
            response.Object = _db.AlunoLists.AsNoTracking().FirstOrDefault(a => a.Id == aluno.Id);
        }
        catch (Exception ex) {
            response.Message = $"Falha ao ativar/desativar aluno: {ex}";
        }

        return response;
    }

    public ResponseModel GetProfileImage(int alunoId) {
        ResponseModel response = new() { Success = false };

        try {
            Aluno? aluno = _db.Alunos.Find(alunoId);

            if (aluno is null) {
                return new ResponseModel { Message = "Aluno não encontrado" };
            }

            response.Success = true;
            response.Message = "Imagem de perfil encontrada";
            response.Object = aluno.Aluno_Foto;
        }
        catch (Exception ex) {
            response.Message = $"Falha ao resgatar imagem de perfil do aluno: {ex}";
        }

        return response;
    }

    public ResponseModel Reposicao(ReposicaoRequest model) {
        ResponseModel response = new() { Success = false };

        try {
            Aluno? aluno = _db.Alunos
                .Include(a => a.Pessoa)
                .FirstOrDefault(a => a.Id == model.Aluno_Id);

            if (aluno is null) {
                return new ResponseModel { Message = "Aluno não encontrado" };
            }

            if (aluno.Pessoa is null) {
                return new ResponseModel { Message = "Pessoa não encontrada" };
            }

            if (aluno.Active == false) {
                return new ResponseModel { Message = "O aluno está desativado" };
            }

            Evento? eventoSource = _db.Eventos
                .Include(e => e.Evento_Aula)
                .Include(e => e.Evento_Participacao_Alunos)
                .FirstOrDefault(e => e.Id == model.Source_Aula_Id);

            if (eventoSource is null) {
                return new ResponseModel { Message = "Evento original não encontrado" };
            }

            if (eventoSource.Evento_Aula is null) {
                return new ResponseModel { Message = "Aula original não encontrada" };
            }

            Evento? eventoDest = _db.Eventos
                .Include(e => e.Evento_Participacao_Alunos)
                .Include(e => e.Evento_Aula!)
                .ThenInclude(e => e.Turma)
                .FirstOrDefault(e => e.Evento_Aula != null && e.Id == model.Dest_Aula_Id);

            if (eventoDest is null) {
                return new ResponseModel { Message = "Evento destino não encontrada" };
            }

            if (eventoDest.Evento_Aula is null) {
                return new ResponseModel { Message = "Aula destino não encontrada" };
            }

            if (model.Source_Aula_Id == model.Dest_Aula_Id) {
                return new ResponseModel { Message = "Aula original e aula destino não podem ser iguais" };
            }

            if (eventoDest.Evento_Aula.Turma_Id.HasValue) {
                if (eventoSource.Evento_Aula.Turma_Id == eventoDest.Evento_Aula.Turma_Id) {
                    return new ResponseModel { Message = "Aluno não pode repor aula na própria turma" };
                }
            }

            if (eventoDest.Finalizado) {
                return new ResponseModel { Message = "Não é possível marcar reposição para uma aula finalizada" };
            }

            //if (eventoDest.Data < TimeFunctions.HoraAtualBR()) {
            //    return new ResponseModel { Message = "Não é possível marcar reposição para uma aula no passado" };
            //}

            if (eventoDest.Deactivated != null) {
                return new ResponseModel { Message = "Não é possível marcar reposição em uma aula desativada" };
            }

            if (Math.Abs((eventoDest.Data - eventoSource.Data).TotalDays) > 30) {
                return new ResponseModel { Message = "A data da aula destino não pode ultrapassar 30 dias de diferença da aula original" };
            }

            bool registroAlreadyExists = eventoDest.Evento_Participacao_Alunos.Any(p => p.Aluno_Id == aluno.Id);

            if (registroAlreadyExists) {
                return new ResponseModel { Message = "Aluno já está cadastrado no evento destino" };
            }

            // A aula destino e o aluno devem compartilhar pelo menos um perfil cognitivo
            bool perfilCognitivoMatches = _db.Evento_Aula_PerfilCognitivo_Rels
                .Any(ep =>
                    ep.Evento_Aula_Id == eventoDest.Id &&
                    ep.PerfilCognitivo_Id == aluno.PerfilCognitivo_Id);

            if (perfilCognitivoMatches == false) {
                return new ResponseModel { Message = "O perfil cognitivo da aula não é adequado para este aluno" };
            }

            int registrosAtivos = eventoDest.Evento_Participacao_Alunos.Count(p => p.Deactivated == null);

            // O evento deve ter espaço para comportar o aluno
            if (registrosAtivos >= eventoDest.CapacidadeMaximaAlunos) {
                return new ResponseModel { Message = "Esse evento de aula já está em sua capacidade máxima" };
            }

            Evento_Participacao_Aluno? registroSource = eventoSource.Evento_Participacao_Alunos.FirstOrDefault(p =>
                p.Deactivated == null
                && p.Aluno_Id == aluno.Id
                && p.Evento_Id == eventoSource.Id);

            if (registroSource is null) {
                return new ResponseModel { Message = "Registro do aluno não foi encontrado na aula original" };
            }

            if (registroSource.Presente == true) {
                return new ResponseModel { Message = "Não é possível de marcar uma reposição de aula se o aluno estava presente na aula original" };
            }

            // Validations passed

            // Se for a primeira aula do aluno, atualizar a data de primeira aula para a data da aula destino
            if (eventoSource.Id == aluno.PrimeiraAula_Id) {
                aluno.PrimeiraAula_Id = eventoDest.Id;
            }

            _db.Alunos.Update(aluno);

            // Amarrar o novo registro à aula sendo reposta
            Evento_Participacao_Aluno registroDest = new()
            {
                Aluno_Id = aluno.Id,
                Evento_Id = eventoDest.Id,
                ReposicaoDe_Evento_Id = eventoSource.Id,
                Observacao = model.Observacao,
                Apostila_Abaco_Id = aluno.Apostila_Abaco_Id,
                NumeroPaginaAbaco = aluno.NumeroPaginaAbaco,
                Apostila_AH_Id = aluno.Apostila_AH_Id,
                NumeroPaginaAH = aluno.NumeroPaginaAH,
            };

            // Se a reposição for feita após o horário da aula, ocasiona falta
            if (TimeFunctions.HoraAtualBR() > eventoSource.Data) {
                registroSource.Presente = false;
            }

            // Desativar o registro da aula
            registroSource.Deactivated = TimeFunctions.HoraAtualBR();
            registroSource.StatusContato_Id = (int)StatusContato.REPOSICAO_AGENDADA;

            _db.Evento_Participacao_Alunos.Update(registroSource);
            _db.Evento_Participacao_Alunos.Add(registroDest);

            _db.Aluno_Historicos.Add(new Aluno_Historico
            {
                Aluno_Id = aluno.Id,
                Descricao = $"O aluno agendou reposição do dia '{eventoSource.Data:G}' para o dia '{eventoDest.Data:G}' com a turma {eventoDest.Evento_Aula?.Turma_Id.ToString() ?? "Extra"}",
                Account_Id = _account!.Id,
                Data = TimeFunctions.HoraAtualBR(),
            });

            _db.SaveChanges();

            response.Success = true;
            response.Object = _db.CalendarioAlunoLists.FirstOrDefault(r => r.Id == registroDest.Id);
            response.Message = "Reposição agendada com sucesso";
        }
        catch (Exception ex) {
            response.Message = $"Falha ao inserir reposição de aula do aluno: {ex}";
        }

        return response;
    }

    public List<ApostilaList> GetApostilasByAluno(int alunoId) {
        Aluno? aluno = _db.Alunos.Find(alunoId);

        if (aluno is null) {
            throw new Exception("Aluno não encontrado");
        }

        List<ApostilaList> apostilas = _db.ApostilaLists
            .Where(a => a.Apostila_Kit_Id == aluno.Apostila_Kit_Id)
            .ToList();

        return apostilas;
    }

    public ResponseModel GetSummaryByAluno(int alunoId) {
        ResponseModel response = new() { Success = false };

        try {
            Aluno? aluno = _db.Alunos.AsNoTracking().FirstOrDefault(a => a.Id == alunoId);

            if (aluno is null) {
                return new ResponseModel { Message = "Aluno não encontrado" };
            }

            List<CalendarioAlunoList> faltas = _db.CalendarioAlunoLists
                .Where(r => r.Aluno_Id == aluno.Id && r.Presente == false)
                .ToList();

            List<CalendarioAlunoList> presencas = _db.CalendarioAlunoLists
                .Where(r => r.Aluno_Id == aluno.Id && r.Presente == true)
                .ToList();

            List<CalendarioAlunoList> reposicoes = _db.CalendarioAlunoLists
                .Where(r => r.Aluno_Id == aluno.Id && r.ReposicaoDe_Evento_Id != null)
                .ToList();

            List<CalendarioAlunoList> aulasFuturas = _db.CalendarioAlunoLists
                .Where(r => r.Aluno_Id == aluno.Id && r.Presente == null)
                .ToList();

            response.Success = true;
            response.Object = new SummaryModel
            {
                Aluno_Id = aluno.Id,
                Turma_Id = aluno.Turma_Id,

                Faltas = faltas,
                Faltas_Count = faltas.Count,

                Presencas = presencas,
                Presencas_Count = presencas.Count,

                Reposicoes = reposicoes,
                Reposicoes_Count = reposicoes.Count,

                Aulas_Futuras = aulasFuturas,
                Aulas_Futuras_Count = aulasFuturas.Count,
            };
            response.Message = "Sumário foi retornado com sucesso";

        }
        catch (Exception ex) {
            response.Message = $"Falha ao buscar sumário do aluno: {ex}";
        }

        return response;
    }

    public List<AlunoHistoricoList> GetHistoricoById(int alunoId) {
        List<AlunoHistoricoList> historicos = _db.AlunoHistoricoList
            .Where(h => h.Aluno_Id == alunoId)
            .ToList();

        return historicos;

    }

    public List<AlunoChecklistItemList> GetAlunoChecklistItemList(AlunoRequest request) {
        IQueryable<AlunoChecklistItemList> listQueryable = _db.AlunoChecklistItemLists
            .Where(a => a.Finalizado == 0)
            .AsQueryable();

        if (request.Turma_Id.HasValue) {
            listQueryable = listQueryable.Where(a => a.Turma_Id == request.Turma_Id);
        }

        if (request.Professor_Id.HasValue) {
            listQueryable = listQueryable.Where(a => a.Professor_Id == request.Professor_Id);
        }

        if (request.Aluno_Id.HasValue) {
            listQueryable = listQueryable.Where(a => a.Aluno_Id == request.Aluno_Id);
        }

        return listQueryable.ToList();
    }

    public ResponseModel PrimeiraAula(PrimeiraAulaRequest model) {
        ResponseModel response = new() { Success = false };

        try {
            Aluno? aluno = _db.Alunos
                .Include(a => a.PrimeiraAula)
                .FirstOrDefault(a => a.Id == model.Aluno_Id);

            if (aluno is null) {
                return new ResponseModel { Message = "Aluno não encontrado" };
            }

            if (aluno.Deactivated != null) {
                return new ResponseModel { Message = "O aluno está desativado" };
            }

            Evento? evento = _db.Eventos
                .Include(e => e.Evento_Participacao_Alunos)
                .FirstOrDefault(e => e.Id == model.Evento_Id);

            if (evento is null) {
                return new ResponseModel { Message = "Evento não encontrado" };
            }

            if (evento.Finalizado == true) {
                return new ResponseModel { Message = "Não foi possível continuar. Este evento já está finalizado." };
            }

            if (evento.Deactivated != null) {
                return new ResponseModel { Message = "Não foi possível continuar. Este evento se encontra desativado." };
            }

            if (aluno.PrimeiraAula != null) {
                return new ResponseModel { Message = $"Aluno já possui uma primeira aula marcada dia: {aluno.PrimeiraAula.Data}" };
            }

            // O aluno deve se encaixar em um dos perfis cognitivos do evento
            bool perfilCognitivoMatches = _db.Evento_Aula_PerfilCognitivo_Rels
                .Any(ep =>
                    ep.Evento_Aula_Id == evento.Id &&
                    ep.PerfilCognitivo_Id == aluno.PerfilCognitivo_Id);

            if (perfilCognitivoMatches == false) {
                return new ResponseModel { Message = "O perfil cognitivo da aula não é adequado para este aluno" };
            }

            int registrosAtivos = evento.Evento_Participacao_Alunos.Count(p => p.Deactivated == null);

            // O evento deve ter espaço para comportar o aluno
            if (registrosAtivos >= evento.CapacidadeMaximaAlunos) {
                return new ResponseModel { Message = "Esse evento já está em sua capacidade máxima" };
            }

            // Se o aluno já estiver no evento, precisa apenas marcar como primeira aula
            bool alunoIsAlreadyInEvent = evento.Evento_Participacao_Alunos.Any(a => a.Aluno_Id == aluno.Id);

            // Validations passed
            if (!alunoIsAlreadyInEvent) {
                Evento_Participacao_Aluno participacao = new()
                {
                    Aluno_Id = aluno.Id,
                    Evento_Id = evento.Id,
                    Apostila_Abaco_Id = aluno.Apostila_Abaco_Id,
                    NumeroPaginaAbaco = aluno.NumeroPaginaAbaco,
                    Apostila_AH_Id = aluno.Apostila_AH_Id,
                    NumeroPaginaAH = aluno.NumeroPaginaAH,
                };

                _db.Evento_Participacao_Alunos.Add(participacao);
            }

            Aluno_Historico historico = new()
            {
                Aluno_Id = aluno.Id,
                Descricao = $"O aluno teve primeira aula agendada para o dia '{evento.Data:G}'",
                Account_Id = _account!.Id,
                Data = TimeFunctions.HoraAtualBR(),
            };

            _db.Aluno_Historicos.Add(historico);

            aluno.PrimeiraAula_Id = evento.Id;
            _db.Alunos.Update(aluno);

            _db.SaveChanges();

            response.Success = true;
            response.Object = _db.CalendarioAlunoLists.FirstOrDefault(r => r.Id == aluno.Id);
            response.Message = "Primeira aula agendada com sucesso";
        }
        catch (Exception ex) {
            response.Message = $"Falha ao inserir primeira aula do aluno: {ex}";
        }

        return response;
    }
}
