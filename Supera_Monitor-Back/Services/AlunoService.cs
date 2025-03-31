using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Aluno;
using Supera_Monitor_Back.Models.Pessoa;
using Supera_Monitor_Back.Services.Email;
using Supera_Monitor_Back.Services.Email.Models;

namespace Supera_Monitor_Back.Services {
    public interface IAlunoService {
        AlunoList Get(int alunoId);
        List<AlunoList> GetAll();
        List<AlunoListWithChecklist> GetAllWithChecklist();
        ResponseModel Insert(CreateAlunoRequest model);
        ResponseModel Update(UpdateAlunoRequest model);
        ResponseModel ToggleDeactivate(int alunoId);

        ResponseModel GetProfileImage(int alunoId);
        ResponseModel GetSummaryByAluno(int alunoId);
        List<ApostilaList> GetApostilasByAluno(int alunoId);

        List<Aluno_Historico> GetHistoricoById(int alunoId);

        ResponseModel NewReposicao(NewReposicaoRequest model);
    }

    public class AlunoService : IAlunoService {
        private readonly DataContext _db;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly IPessoaService _pessoaService;
        private readonly IChecklistService _checklistService;

        private readonly Account? _account;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AlunoService(DataContext db, IMapper mapper, IPessoaService pessoaService, IHttpContextAccessor httpContextAccessor, IChecklistService checklistService, IEmailService emailService)
        {
            _db = db;
            _mapper = mapper;
            _emailService = emailService;
            _pessoaService = pessoaService;
            _checklistService = checklistService;

            _httpContextAccessor = httpContextAccessor;
            _account = ( Account? )httpContextAccessor?.HttpContext?.Items["Account"];
        }

        public AlunoList Get(int alunoId)
        {
            AlunoList? aluno = _db.AlunoLists.AsNoTracking().SingleOrDefault(a => a.Id == alunoId);

            if (aluno is null) {
                throw new Exception("Aluno não encontrado");
            }

            aluno.Restricoes = _db.AlunoRestricaoLists.Where(ar => ar.Aluno_Id == aluno.Id).ToList();

            return aluno;
        }

        public List<AlunoList> GetAll()
        {
            List<AlunoList> alunos = _db.AlunoLists.OrderBy(a => a.Nome).ToList();

            foreach (var aluno in alunos) {
                //var restricoes = _db.Aluno_Restricao_Rels.Where(ar => ar.Aluno_Id == aluno.Id).ToList();
                //aluno.Restricoes = _db.AlunoRestricaoLists.Where(ar => ar.Aluno_Id == aluno.Id).ToList();
            }

            return alunos;
        }

        public List<AlunoListWithChecklist> GetAllWithChecklist()
        {
            List<AlunoList> alunos = _db.AlunoLists.OrderBy(a => a.Nome).ToList();

            List<AlunoListWithChecklist> alunosWithChecklist = _mapper.Map<List<AlunoListWithChecklist>>(alunos);

            foreach (var alunoList in alunosWithChecklist) {
                alunoList.AlunoChecklist = _checklistService.GetAllByAlunoId(alunoList.Id);
            }

            return alunosWithChecklist;
        }

        public ResponseModel Insert(CreateAlunoRequest model)
        {
            ResponseModel response = new() { Success = false };

            try {
                Pessoa? pessoa = _db.Pessoas.Find(model.Pessoa_Id);

                // Aluno só pode ser cadastrado se a pessoa existir no CRM
                if (pessoa == null) {
                    return new ResponseModel { Message = "Pessoa não encontrada" };
                }

                // Aluno só pode ser cadastrado se tiver status matriculado
                if (pessoa.Pessoa_Status_Id != ( int )PessoaStatus.Matriculado) {
                    return new ResponseModel { Message = "Pessoa não está matriculada" };
                }

                // Aluno só pode ser cadastrado se tiver Unidade_Id = 1 (dev)
                if (pessoa.Unidade_Id != 1) {
                    return new ResponseModel { Message = "Pessoa não tem Unidade_Id = 1" };
                }

                // Só pode ser cadastrado um aluno por pessoa
                bool alunoAlreadyRegistered = _db.Alunos.Any(a => a.Pessoa_Id == model.Pessoa_Id);

                if (alunoAlreadyRegistered) {
                    return new ResponseModel { Message = "Pessoa já tem um aluno cadastrado em seu nome" };
                }

                // Aluno só pode ser cadastrado em uma turma válida
                bool turmaExists = _db.Turmas.Any(t => t.Id == model.Turma_Id);

                if (!turmaExists) {
                    return new ResponseModel { Message = "Turma não encontrada" };
                }

                // Aluno só pode ser inserido em uma turma válida
                Turma? turmaDestino = _db.Turmas
                    .Include(t => t.Alunos)
                    .FirstOrDefault(t => t.Id == model.Turma_Id);

                if (turmaDestino is null) {
                    return new ResponseModel { Message = "Turma não encontrada" };
                }

                // Aluno só pode ser inserido em uma turma que não está cheia
                // Não considera reposição, pois não estamos olhando para uma aula específica
                int amountOfAlunosInTurmaDestino = turmaDestino.Alunos.Count;

                if (amountOfAlunosInTurmaDestino >= turmaDestino.CapacidadeMaximaAlunos) {
                    return new ResponseModel { Message = "Turma já está em sua capacidade máxima" };
                }

                // O aluno não pode receber um kit que não existe, mas pode receber kit nulo
                if (model.Apostila_Kit_Id is not null) {
                    bool apostilaKitExists = _db.Apostila_Kits.Any(k => k.Id == model.Apostila_Kit_Id);

                    if (!apostilaKitExists) {
                        return new ResponseModel { Message = "Não é possível inserir um aluno com um kit que não existe" };
                    }
                }

                // Não deve ser possível inserir um aluno com um perfil cognitivo que não existe
                bool perfilCognitivoExists = _db.PerfilCognitivos.Any(p => p.Id == model.PerfilCognitivo_Id);

                if (perfilCognitivoExists == false) {
                    return new ResponseModel { Message = "Não é possível inserir um aluno com um perfil cognitivo que não existe" };
                }

                // Validations passed

                // quando chegar o dia que isso dê pau, me perdoe. só deus sabe como tá a mente do palhaço
                var randomNumberGenerator = new Random();
                int randomNumber;

                do {
                    randomNumber = randomNumberGenerator.Next(100000, 1000000);
                } while (_db.Alunos.Any(a => a.RM == randomNumber.ToString()));

                Aluno aluno = new() {
                    Aluno_Foto = model.Aluno_Foto,
                    DataInicioVigencia = model.DataInicioVigencia,
                    DataFimVigencia = model.DataFimVigencia,
                    Turma_Id = turmaDestino.Id,
                    PerfilCognitivo_Id = model.PerfilCognitivo_Id,

                    Apostila_Kit_Id = model.Apostila_Kit_Id,

                    RM = randomNumber.ToString(),
                    LoginApp = pessoa.Email ?? $"{randomNumber}@supera",
                    SenhaApp = "Super@123",
                    Pessoa_Id = model.Pessoa_Id,

                    Created = TimeFunctions.HoraAtualBR(),
                    LastUpdated = null,
                    Deactivated = null,
                    AspNetUsers_Created_Id = model.AspNetUsers_Created_Id,
                };

                _db.Add(aluno);
                _db.SaveChanges();

                ResponseModel populateChecklistResponse = _checklistService.PopulateAlunoChecklist(aluno.Id);

                if (populateChecklistResponse.Success == false) {
                    return populateChecklistResponse;
                }

                List<Evento> eventoAulasTurmaDestino = _db.Eventos
                    .Include(e => e.Evento_Aula)
                    .Include(e => e.Evento_Participacao_AlunoEventos)
                    .Where(e =>
                        e.Evento_Aula != null
                        && e.Data >= TimeFunctions.HoraAtualBR()
                        && e.Evento_Aula.Turma_Id == aluno.Turma_Id)
                    .ToList();

                // Inserir novos registros deste aluno nas aulas futuras da turma destino
                foreach (Evento evento in eventoAulasTurmaDestino) {
                    // Aula não deve registrar aluno se estiver em sua capacidade máxima e nesse caso, -> considera os alunos de reposição <-
                    var alunosInEventoAula = evento.Evento_Participacao_AlunoEventos.Count(p => p.Deactivated != null);

                    if (alunosInEventoAula >= evento.Evento_Aula!.CapacidadeMaximaAlunos) {
                        continue;
                    }

                    Evento_Participacao_Aluno participacaoAluno = new() {
                        Aluno_Id = aluno.Id,
                        Evento_Id = evento.Id,
                    };

                    _db.Evento_Participacao_Alunos.Add(participacaoAluno);
                }

                _db.Aluno_Historicos.Add(new Aluno_Historico {
                    Aluno_Id = aluno.Id,
                    Descricao = "Aluno cadastrado",
                    Account_Id = _account.Id,
                    Data = TimeFunctions.HoraAtualBR(),
                });

                _db.SaveChanges();

                response.Message = "Aluno cadastrado com sucesso";
                response.Object = _db.AlunoLists.AsNoTracking().FirstOrDefault(a => a.Id == aluno.Id);
                response.Success = true;
            } catch (Exception ex) {
                response.Message = "Falha ao registrar aluno: " + ex.ToString();
            }

            return response;
        }

        public ResponseModel Update(UpdateAlunoRequest model)
        {
            ResponseModel response = new() { Success = false };

            try {

                Aluno? aluno = _db.Alunos.Find(model.Id);

                if (aluno == null) {
                    return new ResponseModel { Message = "Aluno não encontrado" };
                }

                Pessoa? pessoa = _db.Pessoas
                    .Include(p => p.Alunos)
                    .Single(p => p.Id == aluno.Pessoa_Id);

                if (aluno == null) {
                    return new ResponseModel { Message = "Aluno não encontrado" };
                }

                // Não deve ser possível atualizar um aluno com um perfil cognitivo que não existe
                bool perfilCognitivoExists = _db.PerfilCognitivos.Any(p => p.Id == model.PerfilCognitivo_Id);

                if (perfilCognitivoExists == false) {
                    return new ResponseModel { Message = "Não é possível atualizar um aluno com um perfil cognitivo que não existe" };
                }

                // Aluno só pode ser operado em atualizações ou troca de turma se for uma turma válida
                Turma? turmaDestino = _db.Turmas.FirstOrDefault(t => t.Id == model.Turma_Id);

                if (turmaDestino is null) {
                    return new ResponseModel { Message = "Turma não encontrada" };
                }

                bool isSwitchingTurma = aluno.Turma_Id != turmaDestino.Id;

                // Se aluno estiver trocando de turma, deve-se garantir que a turma destino tem espaço disponível
                // Não considera reposição, pois não estamos olhando para uma aula específica
                if (isSwitchingTurma) {
                    int amountOfAlunosInTurma = _db.Alunos.Count(a => a.Turma_Id == turmaDestino.Id && a.Deactivated == null);

                    if (amountOfAlunosInTurma >= turmaDestino.CapacidadeMaximaAlunos) {
                        return new ResponseModel { Message = "Turma já está em sua capacidade máxima" };
                    }
                }

                // O aluno só pode receber um kit que esteja cadastrado ou nulo
                if (model.Apostila_Kit_Id is not null) {
                    bool apostilaKitExists = _db.Apostila_Kits.Any(k => k.Id == model.Apostila_Kit_Id);

                    if (!apostilaKitExists) {
                        return new ResponseModel { Message = "Não é possível atualizar um aluno com um kit que não existe" };
                    }
                }

                // Validations passed

                Aluno? oldObject = _db.Alunos.AsNoTracking().FirstOrDefault(a => a.Id == model.Id);

                UpdatePessoaRequest updatePessoaModel = _mapper.Map<UpdatePessoaRequest>(model);
                updatePessoaModel.Pessoa_Id = aluno.Pessoa_Id;

                ResponseModel pessoaResponse = _pessoaService.Update(updatePessoaModel);

                if (pessoaResponse.Success == false) {
                    return pessoaResponse;
                }

                aluno.RM = model.RM;
                aluno.LoginApp = model.LoginApp;
                aluno.SenhaApp = model.SenhaApp;
                aluno.PerfilCognitivo_Id = model.PerfilCognitivo_Id;

                aluno.Turma_Id = model.Turma_Id;
                aluno.Aluno_Foto = model.Aluno_Foto;
                aluno.Apostila_Kit_Id = model.Apostila_Kit_Id;
                aluno.DataInicioVigencia = model.DataInicioVigencia ?? aluno.DataInicioVigencia;
                aluno.DataFimVigencia = model.DataFimVigencia ?? aluno.DataFimVigencia;

                aluno.LastUpdated = TimeFunctions.HoraAtualBR();

                _db.Alunos.Update(aluno);
                _db.SaveChanges();

                /*
                 * Se o aluno trocou de turma:
                 * 1. Remover seu registro nas próximas aulas da turma original
                 * 2. Adicionar seu registro nas próximas aulas da turma destino
                */

                // TODO: TIRAR AULA DESSA BOMBA
                if (isSwitchingTurma) {
                    List<Aula> aulasTurmaOriginal = _db.Aulas
                        .Where(a =>
                            a.Turma_Id == oldObject.Turma_Id &&
                            a.Data >= TimeFunctions.HoraAtualBR())
                        .ToList();

                    // Para cada aula da turma original, remover os registros do aluno sendo trocado, se existirem
                    foreach (Aula aula in aulasTurmaOriginal) {
                        Aula_Aluno? registro = _db.Aula_Alunos
                            .FirstOrDefault(a =>
                                a.Aula_Id == aula.Id &&
                                a.Aluno_Id == aluno.Id);

                        if (registro is null) {
                            continue;
                        }

                        _db.Aula_Alunos.Remove(registro);
                    }

                    List<Aula> aulasTurmaDestino = _db.Aulas
                        .Where(x =>
                            x.Turma_Id == aluno.Turma_Id &&
                            x.Data >= TimeFunctions.HoraAtualBR())
                        .ToList();

                    // Inserir novos registros deste aluno nas aulas futuras da turma destino
                    foreach (Aula aula in aulasTurmaDestino) {
                        Aula_Aluno registro = new() {
                            Presente = null,
                            Aluno_Id = aluno.Id,
                            Aula_Id = aula.Id,
                        };

                        // Aula não deve registrar aluno se estiver em sua capacidade máxima e nesse caso, -> considera os alunos de reposição <-
                        int AmountOfAlunosInAula = _db.CalendarioAlunoLists.Count(a => a.Aula_Id == aula.Id);

                        if (AmountOfAlunosInAula >= turmaDestino.CapacidadeMaximaAlunos) {
                            continue;
                        }

                        _db.Aula_Alunos.Add(registro);
                    }
                }

                _db.Aluno_Historicos.Add(new Aluno_Historico {
                    Aluno_Id = aluno.Id,
                    Descricao = $"Aluno foi transferido da turma ID: '{oldObject?.Turma_Id}' para a turma ID: '{aluno.Turma_Id}'",
                    Account_Id = _account.Id,
                    Data = TimeFunctions.HoraAtualBR(),
                });

                _db.SaveChanges();

                response.Message = "Aluno atualizado com sucesso";
                response.OldObject = oldObject;
                response.Object = _db.AlunoLists.AsNoTracking().FirstOrDefault(aluno => aluno.Id == model.Id);
                response.Success = true;
            } catch (Exception ex) {
                response.Message = "Falha ao atualizar aluno: " + ex.ToString();
            }

            return response;
        }

        public ResponseModel ToggleDeactivate(int alunoId)
        {
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

                bool IsAlunoActive = aluno.Deactivated == null;

                aluno.Deactivated = IsAlunoActive ? TimeFunctions.HoraAtualBR() : null;

                _db.Alunos.Update(aluno);

                _db.Aluno_Historicos.Add(new Aluno_Historico {
                    Aluno_Id = aluno.Id,
                    Descricao = $"Aluno {(aluno.Deactivated.HasValue ? "Reativado" : "Desativado")}",
                    Account_Id = _account.Id,
                    Data = TimeFunctions.HoraAtualBR(),
                });

                _db.SaveChanges();

                response.Success = true;
                response.Object = _db.AlunoLists.AsNoTracking().FirstOrDefault(a => a.Id == aluno.Id);
            } catch (Exception ex) {
                response.Message = "Falha ao ativar/desativar aluno: " + ex.ToString();
            }

            return response;
        }

        public ResponseModel GetProfileImage(int alunoId)
        {
            ResponseModel response = new() { Success = false };

            try {
                Aluno? aluno = _db.Alunos.Find(alunoId);

                if (aluno is null) {
                    return new ResponseModel { Message = "Aluno não encontrado" };
                }

                response.Success = true;
                response.Message = "Imagem de perfil encontrada";
                response.Object = aluno.Aluno_Foto;
            } catch (Exception ex) {
                response.Message = "Falha ao resgatar imagem de perfil do aluno: " + ex.ToString();
            }

            return response;
        }

        public ResponseModel NewReposicao(NewReposicaoRequest model)
        {
            ResponseModel response = new() { Success = false };

            try {
                Aluno? aluno = _db.Alunos.Find(model.Aluno_Id);

                if (aluno is null) {
                    return new ResponseModel { Message = "Aluno não encontrado" };
                }

                Pessoa? pessoa = _db.Pessoas.FirstOrDefault(a => a.Id == aluno.Pessoa_Id);

                if (pessoa is null) {
                    return new ResponseModel { Message = "Pessoa não encontrada" };
                }

                if (aluno.Active == false) {
                    return new ResponseModel { Message = "Não é possível marcar reposição para um aluno inativo" };
                }

                Aula? aulaSource = _db.Aulas.Find(model.Source_Aula_Id);
                Aula? aulaDest = _db.Aulas.Find(model.Dest_Aula_Id);

                if (aulaSource is null) {
                    return new ResponseModel { Message = "Aula original não encontrada" };
                }

                if (aulaDest is null) {
                    return new ResponseModel { Message = "Aula destino não encontrada" };
                }

                if (model.Source_Aula_Id == model.Dest_Aula_Id) {
                    return new ResponseModel { Message = "Aula original e aula destino não podem ser iguais" };
                }

                if (aulaSource.Turma_Id == aulaDest.Turma_Id) {
                    return new ResponseModel { Message = "Aluno não pode repor aula na própria turma" };
                }

                if (aulaDest.Finalizada) {
                    return new ResponseModel { Message = "Não é possível marcar reposição para uma aula finalizada" };
                }

                if (aulaDest.Data < TimeFunctions.HoraAtualBR()) {
                    return new ResponseModel { Message = "Não é possível marcar reposição para uma aula no passado" };
                }

                if (aulaDest.Deactivated != null) {
                    return new ResponseModel { Message = "Não é possível marcar reposição em uma aula inativa" };
                }

                if (Math.Abs((aulaDest.Data - aulaSource.Data).TotalDays) > 30) {
                    return new ResponseModel { Message = "A data da aula destino não pode ultrapassar 30 dias de diferença da aula original" };
                }

                // Se for aula independente (aulaDest.Turma_Id == -1), não há restrições na reposição
                Turma? turmaDest = _db.Turmas
                    .Include(p => p.Turma_PerfilCognitivo_Rels)
                    .FirstOrDefault(t => t.Id == aulaDest.Turma_Id);

                // Coletar registros ativos da aula destino
                List<Aula_Aluno> registros = _db.Aula_Alunos
                    .Where(r =>
                        r.Aula_Id == model.Dest_Aula_Id &&
                        r.Deactivated.HasValue)
                    .ToList();

                bool RegistroAlreadyExists = registros.Any(r => r.Aluno_Id == model.Aluno_Id);

                if (RegistroAlreadyExists) {
                    return new ResponseModel { Message = "Aluno já está cadastrado na aula destino" };
                }

                // A aula destino e o aluno devem compartilhar pelo menos um perfil cognitivo
                bool perfilCognitivoMatches = _db.Aula_PerfilCognitivo_Rels
                    .Any(ap =>
                        ap.Aula_Id == aulaDest.Id &&
                        ap.PerfilCognitivo_Id == aluno.PerfilCognitivo_Id);

                if (perfilCognitivoMatches == false) {
                    return new ResponseModel { Message = "O perfil cognitivo da aula não é adequado para este aluno" };
                }

                // Se for aula de uma turma, esta deve ter espaço para comportar o aluno
                if (turmaDest is not null) {
                    if (registros.Count >= turmaDest.CapacidadeMaximaAlunos) {
                        return new ResponseModel { Message = "Essa aula já está em sua capacidade máxima" };
                    }
                }

                Aula_Aluno? registroSource = _db.Aula_Alunos.FirstOrDefault(r =>
                    r.Aluno_Id == model.Aluno_Id &&
                    r.Aula_Id == model.Source_Aula_Id);

                if (registroSource is null) {
                    return new ResponseModel { Message = "Registro do aluno não foi encontrado na aula original" };
                }

                if (registroSource.Presente == true) {
                    return new ResponseModel { Message = "Não é possível de marcar reposição de aula para um aluno se este estava presente na aula original" };
                }

                // Validations passed

                // Amarrar o novo registro à aula sendo reposta
                Aula_Aluno registroDest = new() {
                    Aluno_Id = model.Aluno_Id,
                    Aula_Id = model.Dest_Aula_Id,
                    Presente = null,
                    ReposicaoDe_Aula_Id = model.Source_Aula_Id,
                };

                // Se a reposição for feita após o horário da aula, ocasiona falta
                if (TimeFunctions.HoraAtualBR() > aulaSource.Data) {
                    registroSource.Presente = false;
                }

                // Desativar o registro da aula
                registroSource.Deactivated = TimeFunctions.HoraAtualBR();

                _db.Aula_Alunos.Update(registroSource);

                _db.Aula_Alunos.Add(registroDest);

                _db.Aluno_Historicos.Add(new Aluno_Historico {
                    Aluno_Id = aluno.Id,
                    Descricao = $"Aluno realizou reposição da aula original ID: '{aulaSource.Id}' para aula destino ID: '{aulaDest.Id}'",
                    Account_Id = _account.Id,
                    Data = TimeFunctions.HoraAtualBR(),
                });

                _db.SaveChanges();

                // Enviar, de forma assíncrona, e-mail aos interessados:
                // 1. O professor responsável pela aula
                // 2. Ao aluno que teve a aula reposta

                Professor? professor = _db.Professors
                    .Include(p => p.Account)
                    .FirstOrDefault(p => p.Id == aulaSource.Professor_Id);

                if (professor is not null) {
                    // TODO: Em produção, alterar o destinatário do e-mail

                    //_emailService.SendEmail(
                    //    templateType: "ReposicaoAula",
                    //    model: new ReposicaoEmailModel { },
                    //    to: professor.Account.Email
                    //);

                    List<int> alunoIdsInAulaDest = _db.Aula_Alunos
                        .Where(r => r.Aula_Id == aulaDest.Id)
                        .Select(r => r.Aluno_Id)
                        .ToList();

                    List<int> pessoaIdsInAulaDest = _db.Alunos
                        .Where(a => alunoIdsInAulaDest.Contains(a.Id))
                        .Select(a => a.Pessoa_Id)
                        .ToList();

                    List<Pessoa> pessoasInAulaDest = _db.Pessoas
                        .Where(a => pessoaIdsInAulaDest.Contains(a.Id))
                        .ToList();

                    _emailService.SendEmail(
                        templateType: "ReposicaoProfessor",
                        model: new ProfessorReposicaoEmailModel {
                            Name = professor.Account.Name,
                            AlunoName = pessoa.Nome ?? "Nome não encontrado",
                            NewDate = aulaDest.Data,
                            OldDate = aulaSource.Data,
                            Pessoas = pessoasInAulaDest,
                            TurmaName = turmaDest is not null ? turmaDest.Nome : "Aula independente"
                        },
                        to: "noemi@bullest.com.br"
                    );

                    _emailService.SendEmail(
                        templateType: "ReposicaoProfessor",
                        model: new ProfessorReposicaoEmailModel {
                            Name = professor.Account.Name,
                            AlunoName = pessoa.Nome ?? "Nome não encontrado",
                            NewDate = aulaDest.Data,
                            OldDate = aulaSource.Data,
                            Pessoas = pessoasInAulaDest,
                            TurmaName = turmaDest is not null ? turmaDest.Nome : "Aula independente"
                        },
                        to: "lgalax1y@gmail.com"
                    );
                }

                //_emailService.SendEmail(
                //    templateType: "ReposicaoAluno",
                //    model: new AlunoReposicaoEmailModel {
                //        Name = pessoa.Nome ?? "Nome não encontrado",
                //        OldDate = aulaSource.Data,
                //        NewDate = aulaDest.Data,
                //        TurmaName = turmaDest is not null ? turmaDest.Nome : "Aula independente"
                //    },
                //    to: pessoa.Email!
                //);

                _emailService.SendEmail(
                    templateType: "ReposicaoAluno",
                    model: new AlunoReposicaoEmailModel {
                        Name = pessoa.Nome ?? "Nome não encontrado",
                        OldDate = aulaSource.Data,
                        NewDate = aulaDest.Data,
                        TurmaName = turmaDest is not null ? turmaDest.Nome : "Aula independente"
                    },
                    to: "lgalax1y@gmail.com"
                );

                _emailService.SendEmail(
                    templateType: "ReposicaoAluno",
                    model: new AlunoReposicaoEmailModel {
                        Name = pessoa.Nome ?? "Nome não encontrado",
                        OldDate = aulaSource.Data,
                        NewDate = aulaDest.Data,
                        TurmaName = turmaDest is not null ? turmaDest.Nome : "Aula independente"
                    },
                    to: "noemi@bullest.com.br"
                );

                response.Success = true;
                response.Object = _db.CalendarioAlunoLists.FirstOrDefault(r => r.Id == registroDest.Id);
                response.Message = "Reposição agendada com sucesso";
            } catch (Exception ex) {
                response.Message = "Falha ao inserir reposição de aula do aluno: " + ex.ToString();
            }

            return response;
        }

        public List<ApostilaList> GetApostilasByAluno(int alunoId)
        {
            Aluno? aluno = _db.Alunos.Find(alunoId);

            if (aluno is null) {
                throw new Exception("Aluno não encontrado");
            }

            List<ApostilaList> apostilas = _db.ApostilaLists
                .Where(a => a.Apostila_Kit_Id == aluno.Apostila_Kit_Id)
                .ToList();

            return apostilas;
        }

        public ResponseModel GetSummaryByAluno(int alunoId)
        {
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
                    .Where(r => r.Aluno_Id == aluno.Id && r.ReposicaoDe_Aula_Id != null)
                    .ToList();

                List<CalendarioAlunoList> aulasFuturas = _db.CalendarioAlunoLists
                    .Where(r => r.Aluno_Id == aluno.Id && r.Presente == null)
                    .ToList();

                response.Success = true;
                response.Object = new SummaryModel {
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

            } catch (Exception ex) {
                response.Message = "Falha ao buscar sumário do aluno: " + ex.ToString();
            }

            return response;
        }

        public List<Aluno_Historico> GetHistoricoById(int alunoId)
        {
            return _db.Aluno_Historicos.Where(h => h.Aluno_Id == alunoId).ToList();
        }
    }
}
