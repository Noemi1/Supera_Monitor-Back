using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Aluno;
using Supera_Monitor_Back.Models.Pessoa;
using static Supera_Monitor_Back.Entities.Pessoa_Status;

namespace Supera_Monitor_Back.Services {
    public interface IAlunoService {
        AlunoList Get(int alunoId);
        List<AlunoList> GetAll();
        ResponseModel Insert(CreateAlunoRequest model);
        ResponseModel Update(UpdateAlunoRequest model);
        ResponseModel ToggleDeactivate(int alunoId);

        ResponseModel GetProfileImage(int alunoId);
        ResponseModel GetSummaryByAluno(int alunoId);
        List<ApostilaList> GetApostilasByAluno(int alunoId);

        ResponseModel NewReposicao(NewReposicaoRequest model);

    }

    public class AlunoService : IAlunoService {
        private readonly DataContext _db;
        private readonly IMapper _mapper;
        private readonly Account? _account;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IPessoaService _pessoaService;

        public AlunoService(DataContext db, IMapper mapper, IPessoaService pessoaService, IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _mapper = mapper;
            _pessoaService = pessoaService;
            _httpContextAccessor = httpContextAccessor;
            _account = ( Account? )httpContextAccessor?.HttpContext?.Items["Account"];
        }

        public AlunoList Get(int alunoId)
        {
            AlunoList? aluno = _db.AlunoList.AsNoTracking().SingleOrDefault(a => a.Id == alunoId);

            if (aluno is null) {
                throw new Exception("Aluno não encontrado");
            }

            return aluno;
        }

        public List<AlunoList> GetAll()
        {
            List<AlunoList> alunos = _db.AlunoList.OrderBy(a => a.Nome).ToList();

            return alunos;
        }

        public ResponseModel Insert(CreateAlunoRequest model)
        {
            ResponseModel response = new() { Success = false };

            try {
                Pessoa? pessoa = _db.Pessoa.Find(model.Pessoa_Id);

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
                bool AlunoAlreadyRegistered = _db.Aluno.Any(a => a.Pessoa_Id == model.Pessoa_Id);

                if (AlunoAlreadyRegistered) {
                    return new ResponseModel { Message = "Pessoa já tem um aluno cadastrado em seu nome" };
                }

                // Aluno só pode ser cadastrado em uma turma válida
                bool TurmaExists = _db.Turma.Any(t => t.Id == model.Turma_Id);

                if (!TurmaExists) {
                    return new ResponseModel { Message = "Turma não encontrada" };
                }

                // Aluno só pode ser inserido em uma turma válida
                Turma? turmaDestino = _db.Turma.FirstOrDefault(t => t.Id == model.Turma_Id);

                if (turmaDestino is null) {
                    return new ResponseModel { Message = "Turma não encontrada" };
                }

                // Aluno só pode ser inserido em uma turma que não está cheia
                // Não considera reposição, pois não estamos olhando para uma aula específica
                int AmountOfAlunosInTurma = _db.AlunoList.Count(a => a.Turma_Id == turmaDestino.Id);

                if (AmountOfAlunosInTurma >= turmaDestino.CapacidadeMaximaAlunos) {
                    return new ResponseModel { Message = "Turma já está em sua capacidade máxima" };
                }

                // O aluno só pode receber um kit que esteja cadastrado ou nulo
                if (model.Apostila_Kit_Id is not null) {
                    bool apostilaKitExists = _db.Apostila_Kit.Any(k => k.Id == model.Apostila_Kit_Id);

                    if (!apostilaKitExists) {
                        return new ResponseModel { Message = "Não é possível atualizar um aluno com um kit que não existe" };
                    }
                }

                // Validations passed

                Aluno aluno = _mapper.Map<Aluno>(model);

                aluno.Apostila_Kit_Id = model.Apostila_Kit_Id;
                aluno.Aluno_Foto = model.Aluno_Foto;
                aluno.Created = TimeFunctions.HoraAtualBR();
                aluno.Deactivated = null;

                _db.Aluno.Add(aluno);
                _db.SaveChanges();

                List<Aula> aulasTurmaDestino = _db.Aula
                    .Where(x =>
                        x.Turma_Id == aluno.Turma_Id &&
                        x.Data >= TimeFunctions.HoraAtualBR())
                    .ToList();

                // Inserir novos registros deste aluno nas aulas futuras da turma destino
                foreach (Aula aula in aulasTurmaDestino) {
                    // Aula não deve registrar aluno se estiver em sua capacidade máxima e nesse caso, -> considera os alunos de reposição <-
                    int AmountOfAlunosInAula = _db.CalendarioAlunoList.Count(a => a.Aula_Id == aula.Id);

                    if (AmountOfAlunosInAula >= turmaDestino.CapacidadeMaximaAlunos) {
                        continue;
                    }

                    Aula_Aluno registro = new() {
                        Aluno_Id = aluno.Id,
                        Aula_Id = aula.Id,
                        Presente = null,
                    };

                    _db.Aula_Aluno.Add(registro);
                }

                _db.SaveChanges();

                response.Message = "Aluno cadastrado com sucesso";
                response.Object = _db.AlunoList.AsNoTracking().FirstOrDefault(a => a.Id == aluno.Id);
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
                Aluno? aluno = _db.Aluno.Find(model.Id);

                // Aluno só pode ser atualizado se existir
                if (aluno == null) {
                    return new ResponseModel { Message = "Aluno não encontrado" };
                }

                // Aluno só pode ser operado em atualizações ou troca de turma se for uma turma válida
                Turma? turmaDestino = _db.Turma.FirstOrDefault(t => t.Id == model.Turma_Id);

                if (turmaDestino is null) {
                    return new ResponseModel { Message = "Turma não encontrada" };
                }

                bool isSwitchingTurma = aluno.Turma_Id != turmaDestino.Id;

                // Se aluno estiver trocando de turma, deve-se garantir que a turma destino tem espaço disponível
                // Não considera reposição, pois não estamos olhando para uma aula específica
                if (isSwitchingTurma) {
                    int AmountOfAlunosInTurma = _db.AlunoList.Count(a => a.Turma_Id == turmaDestino.Id);

                    if (AmountOfAlunosInTurma >= turmaDestino.CapacidadeMaximaAlunos) {
                        return new ResponseModel { Message = "Turma já está em sua capacidade máxima" };
                    }
                }

                // O aluno só pode receber um kit que esteja cadastrado ou nulo
                if (model.Apostila_Kit_Id is not null) {
                    bool apostilaKitExists = _db.Apostila_Kit.Any(k => k.Id == model.Apostila_Kit_Id);

                    if (!apostilaKitExists) {
                        return new ResponseModel { Message = "Não é possível atualizar um aluno com um kit que não existe" };
                    }
                }

                AlunoList? old = _db.AlunoList.AsNoTracking().FirstOrDefault(a => a.Id == model.Id);

                if (old is null) {
                    return new ResponseModel { Message = "Aluno original não encontrado" };
                }

                UpdatePessoaRequest updatePessoaModel = _mapper.Map<UpdatePessoaRequest>(model);
                updatePessoaModel.Pessoa_Id = aluno.Pessoa_Id;

                ResponseModel pessoaResponse = _pessoaService.Update(updatePessoaModel);

                if (pessoaResponse.Success == false) {
                    return pessoaResponse;
                }

                aluno.Apostila_Kit_Id = model.Apostila_Kit_Id;
                aluno.Aluno_Foto = model.Aluno_Foto;
                aluno.Turma_Id = model.Turma_Id;
                aluno.LastUpdated = TimeFunctions.HoraAtualBR();

                _db.Aluno.Update(aluno);
                _db.SaveChanges();

                /*
                 * Se o aluno trocou de turma:
                 * 1. Remover seu registro nas próximas aulas da turma original
                 * 2. Adicionar seu registro nas próximas aulas da turma destino
                */
                if (isSwitchingTurma) {
                    List<Aula> aulasTurmaOriginal = _db.Aula
                        .Where(a =>
                            a.Turma_Id == old.Turma_Id &&
                            a.Data >= TimeFunctions.HoraAtualBR())
                        .ToList();

                    // Para cada aula da turma original, remover os registros do aluno sendo trocado, se existirem
                    foreach (Aula aula in aulasTurmaOriginal) {
                        Aula_Aluno? registro = _db.Aula_Aluno
                            .FirstOrDefault(a =>
                                a.Aula_Id == aula.Id &&
                                a.Aluno_Id == aluno.Id);

                        if (registro is null) {
                            continue;
                        }

                        _db.Aula_Aluno.Remove(registro);
                    }

                    List<Aula> aulasTurmaDestino = _db.Aula
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
                        int AmountOfAlunosInAula = _db.CalendarioAlunoList.Count(a => a.Aula_Id == aula.Id);

                        if (AmountOfAlunosInAula >= turmaDestino.CapacidadeMaximaAlunos) {
                            continue;
                        }

                        _db.Aula_Aluno.Add(registro);
                    }
                }

                _db.SaveChanges();

                response.Message = "Aluno atualizado com sucesso";
                response.OldObject = old;
                response.Object = _db.AlunoList.AsNoTracking().FirstOrDefault(aluno => aluno.Id == model.Id);
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
                Aluno? aluno = _db.Aluno.Find(alunoId);

                if (aluno == null) {
                    return new ResponseModel { Message = "Aluno não encontrado." };
                }

                if (_account == null) {
                    return new ResponseModel { Message = "Não foi possível completar a ação. Autenticação do autor não encontrada." };
                }

                // Validations passed

                bool IsAlunoActive = aluno.Deactivated == null;

                aluno.Deactivated = IsAlunoActive ? TimeFunctions.HoraAtualBR() : null;

                _db.Aluno.Update(aluno);
                _db.SaveChanges();

                response.Success = true;
                response.Object = _db.AlunoList.AsNoTracking().FirstOrDefault(a => a.Id == aluno.Id);
            } catch (Exception ex) {
                response.Message = "Falha ao ativar/desativar aluno: " + ex.ToString();
            }

            return response;
        }

        public ResponseModel GetProfileImage(int alunoId)
        {
            ResponseModel response = new() { Success = false };

            try {
                Aluno? aluno = _db.Aluno.Find(alunoId);

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
                Aluno? aluno = _db.Aluno.Find(model.Aluno_Id);

                if (aluno is null) {
                    return new ResponseModel { Message = "Aluno não encontrado" };
                }

                if (aluno.Active == false) {
                    return new ResponseModel { Message = "Não é possível marcar reposição para um aluno inativo" };
                }

                Aula? aulaSource = _db.Aula.Find(model.Source_Aula_Id);
                Aula? aulaDest = _db.Aula.Find(model.Dest_Aula_Id);

                if (aulaSource is null) {
                    return new ResponseModel { Message = "Aula original não encontrada" };
                }

                if (aulaDest is null) {
                    return new ResponseModel { Message = "Aula destino não encontrada" };
                }

                if (model.Source_Aula_Id == model.Dest_Aula_Id) {
                    return new ResponseModel { Message = "Aula original e aula destino não podem ser iguais" };
                }

                if (aulaDest.Data < TimeFunctions.HoraAtualBR()) {
                    return new ResponseModel { Message = "Não é possível marcar reposição para uma aula que já ocorreu no passado" };
                }

                if (Math.Abs((aulaDest.Data - aulaSource.Data).TotalDays) > 30) {
                    return new ResponseModel { Message = "A data da aula destino não pode ultrapassar 30 dias de diferença da aula original" };
                }

                Turma? turmaSource = _db.Turma.Find(aulaSource.Turma_Id);
                Turma? turmaDest = _db.Turma.Find(aulaDest.Turma_Id);

                if (turmaSource is null) {
                    return new ResponseModel { Message = "Turma original não encontrada" };
                }

                if (turmaDest is null) {
                    return new ResponseModel { Message = "Turma destino não encontrada" };
                }

                //if (turmaSource.Turma_Tipo_Id != turmaDest.Turma_Tipo_Id) {
                //    return new ResponseModel { Message = "Não é possível repor aulas em uma turma de outro tipo" };
                //}

                Aula_Aluno? registroSource = _db.Aula_Aluno.FirstOrDefault(r =>
                    r.Aluno_Id == model.Aluno_Id &&
                    r.Aula_Id == model.Source_Aula_Id);

                if (registroSource is null) {
                    return new ResponseModel { Message = "Registro do aluno não foi encontrado na aula original" };
                }

                List<CalendarioAlunoList> registros = _db.CalendarioAlunoList.Where(r => r.Aula_Id == model.Dest_Aula_Id).ToList();

                bool ReposicaoAlreadyExists = registros.Any(r => r.Aluno_Id == model.Aluno_Id);

                if (ReposicaoAlreadyExists) {
                    return new ResponseModel { Message = "Aluno já está cadastrado para reposição nesta aula" };
                }

                if (registros.Count >= turmaDest.CapacidadeMaximaAlunos) {
                    return new ResponseModel { Message = "Essa aula já está em sua capacidade máxima" };
                }

                // Validations passed

                Aula_Aluno registroDest = new() {
                    Aluno_Id = model.Aluno_Id,
                    Aula_Id = model.Dest_Aula_Id,
                    Presente = null,
                    ReposicaoDe_Aula_Id = model.Source_Aula_Id,
                };

                _db.Aula_Aluno.Add(registroDest);
                _db.SaveChanges();

                _db.Aula_Aluno.Remove(registroSource);
                _db.SaveChanges();

                response.Success = true;
                response.Object = _db.CalendarioAlunoList.FirstOrDefault(r => r.Id == registroDest.Id);
                response.Message = "Reposição agendada com sucesso";
            } catch (Exception ex) {
                response.Message = "Falha ao inserir reposição de aula do aluno: " + ex.ToString();
            }

            return response;
        }

        public List<ApostilaList> GetApostilasByAluno(int alunoId)
        {
            Aluno? aluno = _db.Aluno.Find(alunoId);

            if (aluno is null) {
                throw new Exception("Aluno não encontrado");
            }

            List<ApostilaList> apostilas = _db.ApostilaList
                .Where(a => a.Apostila_Kit_Id == aluno.Apostila_Kit_Id)
                .ToList();

            return apostilas;
        }

        public ResponseModel GetSummaryByAluno(int alunoId)
        {
            ResponseModel response = new() { Success = false };

            try {
                Aluno? aluno = _db.Aluno.AsNoTracking().FirstOrDefault(a => a.Id == alunoId);

                if (aluno is null) {
                    return new ResponseModel { Message = "Aluno não encontrado" };
                }

                List<CalendarioAlunoList> faltas = _db.CalendarioAlunoList
                    .Where(r => r.Aluno_Id == aluno.Id && r.Presente == false)
                    .ToList();

                List<CalendarioAlunoList> presencas = _db.CalendarioAlunoList
                    .Where(r => r.Aluno_Id == aluno.Id && r.Presente == true)
                    .ToList();

                List<CalendarioAlunoList> reposicoes = _db.CalendarioAlunoList
                    .Where(r => r.Aluno_Id == aluno.Id && r.ReposicaoDe_Aula_Id != null)
                    .ToList();

                List<CalendarioAlunoList> aulasFuturas = _db.CalendarioAlunoList
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
    }
}
