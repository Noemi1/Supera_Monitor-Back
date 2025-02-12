using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Aluno;
using Supera_Monitor_Back.Models.Aula;
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
        //Task<ResponseModel> UploadProfileImage();
        //Task<ResponseModel> UploadImage(int alunoId, byte[] BinaryImage);

        ResponseModel InsertReposicao(CreateReposicaoRequest model);
        ResponseModel NewReposicao(NewReposicaoRequest model);
    }

    public class AlunoService : IAlunoService {
        private readonly DataContext _db;
        private readonly IMapper _mapper;
        private readonly Account? _account;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IPessoaService _pessoaService;
        private readonly IAulaService _aulaService;

        public AlunoService(DataContext db, IMapper mapper, IPessoaService pessoaService, IAulaService aulaService, IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _mapper = mapper;
            _pessoaService = pessoaService;
            _aulaService = aulaService;
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
            List<AlunoList> alunos = _db.AlunoList.ToList();

            return alunos;
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
                bool AlunoAlreadyRegistered = _db.Alunos.Any(a => a.Pessoa_Id == model.Pessoa_Id);

                if (AlunoAlreadyRegistered) {
                    return new ResponseModel { Message = "Pessoa já tem um aluno cadastrado em seu nome" };
                }

                // Aluno só pode ser cadastrado em uma turma válida
                bool TurmaExists = _db.Turmas.Any(t => t.Id == model.Turma_Id);

                if (!TurmaExists) {
                    return new ResponseModel { Message = "Turma não encontrada" };
                }

                // Aluno só pode ser inserido em uma turma válida
                Turma? turmaDestino = _db.Turmas.FirstOrDefault(t => t.Id == model.Turma_Id);

                if (turmaDestino is null) {
                    return new ResponseModel { Message = "Turma não encontrada" };
                }

                // Aluno só pode ser inserido em uma turma que não está cheia

                int AmountOfAlunosInTurma = _db.AlunoList.Count(a => a.Turma_Id == turmaDestino.Id);

                if (AmountOfAlunosInTurma >= turmaDestino.CapacidadeMaximaAlunos) {
                    return new ResponseModel { Message = "Turma já está em sua capacidade máxima" };
                }

                // Validations passed
                Aluno aluno = _mapper.Map<Aluno>(model);

                aluno.Aluno_Foto = model.Aluno_Foto;
                aluno.Created = TimeFunctions.HoraAtualBR();
                aluno.Deactivated = null;

                _db.Alunos.Add(aluno);
                _db.SaveChanges();

                // Coleta a lista das aulas futuras desta turma
                List<TurmaAula> aulas = _db.TurmaAulas
                    .Where(x =>
                        x.Turma_Id == aluno.Turma_Id &&
                        x.Data >= DateTime.Now)
                    .Include(x => x.Turma)
                    .ToList();

                // Registrar o aluno nas futuras aulas (aulas que já existem)
                foreach (TurmaAula aula in aulas) {
                    // Não devo conseguir inserir o aluno em uma aula se esta estiver cheia, tanto de reposição quanto de alunos registrados normalmente
                    if (aula.Turma is not null) {
                        // Contar todos os registrados na aula em questão
                        int AmountOfAlunosInAula = _db.TurmaAulaAlunos.Count(a => a.Turma_Aula_Id == aula.Id);

                        // TODO: Se a quantidade de alunos registrados for além do limite, fazer algo (?) - Por enquanto só ignora e continua
                        if (AmountOfAlunosInAula >= aula.Turma.CapacidadeMaximaAlunos) {
                            continue;
                        }
                    }

                    TurmaAulaAluno registro = new() {
                        Aluno_Id = aluno.Id,
                        Turma_Aula_Id = aula.Id,
                        Presente = null,
                    };

                    _db.TurmaAulaAlunos.Add(registro);
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
                Aluno? aluno = _db.Alunos.Find(model.Id);

                // Aluno só pode ser atualizado se existir
                if (aluno == null) {
                    return new ResponseModel { Message = "Aluno não encontrado" };
                }

                // Aluno só pode ser operado em atualizações ou troca de turma se for uma turma válida
                Turma? turmaDestino = _db.Turmas.FirstOrDefault(t => t.Id == model.Turma_Id);

                if (turmaDestino is null) {
                    return new ResponseModel { Message = "Turma não encontrada" };
                }

                bool isSwitchingTurma = aluno.Turma_Id != model.Turma_Id;

                // Se aluno estiver trocando de turma, deve-se garantir que a turma destino tem espaço disponível
                if (isSwitchingTurma) {
                    int AmountOfAlunosInAula = _db.AlunoList.Count(a => a.Turma_Id == turmaDestino.Id);

                    if (AmountOfAlunosInAula >= turmaDestino.CapacidadeMaximaAlunos) {
                        return new ResponseModel { Message = "Turma já está em sua capacidade máxima" };
                    }
                }

                AlunoList? old = _db.AlunoList.AsNoTracking().FirstOrDefault(a => a.Id == model.Id);

                if (old is null) {
                    return new ResponseModel { Message = "Aluno original não encontrado" };
                }

                UpdatePessoaRequest pessoaModel = _mapper.Map<UpdatePessoaRequest>(model);
                pessoaModel.Pessoa_Id = aluno.Pessoa_Id;

                ResponseModel pessoaResponse = _pessoaService.Update(pessoaModel);

                // Caso não tenha passado nas validações de Pessoa
                if (pessoaResponse.Success == false) {
                    return pessoaResponse;
                }

                aluno.Aluno_Foto = model.Aluno_Foto;
                aluno.Turma_Id = model.Turma_Id;
                aluno.LastUpdated = TimeFunctions.HoraAtualBR();

                _db.Alunos.Update(aluno);
                _db.SaveChanges();

                /*
                 * Se o aluno trocou de turma:
                 * 1. Remover seu registro nas próximas aulas da turma original
                 * 2. Adicionar seu registro nas próximas aulas da turma destino
                */
                if (isSwitchingTurma) {
                    List<TurmaAula> originalTurmaAulas = _db.TurmaAulas
                        .Where(x =>
                            x.Turma_Id == old.Turma_Id &&
                            x.Data >= DateTime.Now)
                        .ToList();

                    // Remover registros futuros das aulas originais
                    foreach (TurmaAula aula in originalTurmaAulas) {
                        TurmaAulaAluno? registro = _db.TurmaAulaAlunos
                            .FirstOrDefault(x =>
                                x.Turma_Aula_Id == aula.Id &&
                                x.Aluno_Id == aluno.Id);

                        if (registro is null) {
                            continue;
                        }

                        _db.TurmaAulaAlunos.Remove(registro);

                    }

                    var destinoTurmaAulas = _db.TurmaAulas
                        .Where(x =>
                            x.Turma_Id == aluno.Turma_Id &&
                            x.Data >= DateTime.Now)
                        .ToList();

                    // Adicionar registros na aula destino
                    foreach (TurmaAula aula in destinoTurmaAulas) {
                        TurmaAulaAluno registro = new() {
                            Presente = null,
                            Aluno_Id = aluno.Id,
                            Turma_Aula_Id = aula.Id,
                        };

                        _db.TurmaAulaAlunos.Add(registro);
                    }
                }

                _db.SaveChanges();

                response.Message = "Aluno atualizado com sucesso";
                response.Object = _db.AlunoList.AsNoTracking().FirstOrDefault(aluno => aluno.Id == model.Id);
                response.Success = true;
                response.OldObject = old;
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
                TurmaAula? aulaSource = _db.TurmaAulas.Find(model.Source_Aula_Id);
                TurmaAula? aulaDest = _db.TurmaAulas.Find(model.Dest_Aula_Id);

                if (aulaSource is null) {
                    return new ResponseModel { Message = "Aula original não encontrada" };
                }

                if (aulaDest is null) {
                    return new ResponseModel { Message = "Aula destino não encontrada" };
                }

                Turma? turmaSource = _db.Turmas.Find(aulaSource.Turma_Id);
                Turma? turmaDest = _db.Turmas.Find(aulaDest.Turma_Id);

                if (turmaSource is null) {
                    return new ResponseModel { Message = "Turma original não encontrada" };
                }

                if (turmaDest is null) {
                    return new ResponseModel { Message = "Turma destino não encontrada" };
                }

                if (turmaSource.Turma_Tipo_Id != turmaDest.Turma_Tipo_Id) {
                    return new ResponseModel { Message = "Não é possível repor aulas em uma turma de outro tipo" };
                }

                TurmaAulaAluno? registroSource = _db.TurmaAulaAlunos.FirstOrDefault(r => r.Turma_Aula_Id == aulaSource.Id);

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

                TurmaAulaAluno registroDest = new() {
                    Aluno_Id = model.Aluno_Id,
                    Turma_Aula_Id = model.Dest_Aula_Id,
                    Presente = null,
                    Reposicao = true,
                };

                _db.TurmaAulaAlunos.Add(registroDest);
                _db.SaveChanges();

                _db.TurmaAulaAlunos.Remove(registroSource);
                _db.SaveChanges();

                response.Success = true;
                response.Object = _db.CalendarioAlunoList.FirstOrDefault(r => r.Id == registroDest.Id);
                response.Message = "Reposição agendada com sucesso";
            } catch (Exception ex) {
                response.Message = "Falha ao inserir reposição de aula do aluno: " + ex.ToString();
            }

            return response;
        }


        public ResponseModel InsertReposicao(CreateReposicaoRequest model)
        {
            ResponseModel response = new() { Success = false };

            try {
                AulaList? aulaSource = _db.AulaList.FirstOrDefault(a => a.Id == model.Source_Aula_Id);
                TurmaAulaAluno? registroSource = null;

                // Se aulaSource existe, remover o registro do aluno desta aula
                if (aulaSource is not null) {
                    registroSource = _db.TurmaAulaAlunos.FirstOrDefault(a => a.Turma_Aula_Id == aulaSource.Id);

                    if (registroSource is null) {
                        return new ResponseModel { Message = "Aluno não está cadastrado na aula de origem" };
                    }
                }

                // Se aulaSource não existe, criá-la
                if (aulaSource is null) {
                    if (model.Source_Turma_Id is null) {
                        return new ResponseModel { Message = "Turma inválida" };
                    }

                    if (model.Source_Data is null) {
                        return new ResponseModel { Message = "Turma inválida" };
                    }

                    if (model.Source_Professor_Id is null) {
                        return new ResponseModel { Message = "Turma inválida" };
                    }

                    CreateAulaRequest createAulaRequest = new() {
                        Turma_Id = ( int )model.Source_Turma_Id,
                        Data = ( DateTime )model.Source_Data,
                        Professor_Id = ( int )model.Source_Professor_Id,
                        Observacao = "",
                    };

                    ResponseModel createAulaResponse = _aulaService.Insert(createAulaRequest);

                    if (createAulaResponse.Success == false) {
                        return createAulaResponse;
                    }

                    int createdAulaId = createAulaResponse.Object!.Id;

                    registroSource = _db.TurmaAulaAlunos.FirstOrDefault(a => a.Turma_Aula_Id == createdAulaId);

                    if (registroSource is null) {
                        return new ResponseModel { Message = "Aluno não está cadastrado na aula original recém criada." };
                    }
                }

                if (registroSource is null) {
                    return new ResponseModel { Message = "Ocorreu algum erro ao coletar o registro original do aluno" };
                }


                AulaList? aulaDest = _db.AulaList.FirstOrDefault(a => a.Id == model.Dest_Aula_Id);
                TurmaAulaAluno? registroDest = null;

                // Se aula destino existe, colocar o aluno nela
                if (aulaDest is not null) {
                    Turma? TurmaDestino = _db.Turmas.Find(aulaDest.Turma_Id);

                    if (TurmaDestino is null) {
                        return new ResponseModel { Message = "Turma destino não encontrada" };
                    }

                    // Se aula estiver cheia, não deve ser possível registrar o aluno nela
                    int AlunosInAulaDest = _db.CalendarioAlunoList.Count(a => a.Aula_Id == aulaDest.Id);

                    if (AlunosInAulaDest >= TurmaDestino.CapacidadeMaximaAlunos) {
                        return new ResponseModel { Message = "Aula já está em sua capacidade máxima" };
                    }

                    registroDest = new() {
                        Turma_Aula_Id = aulaDest.Id,
                        Aluno_Id = model.Aluno_Id,
                        Presente = null,
                        Reposicao = true,
                    };
                }

                // Se aula destino não existe, criá-la
                if (aulaDest is null) {
                    if (model.Dest_Turma_Id is null) {
                        return new ResponseModel { Message = "Turma inválida" };
                    }

                    if (model.Dest_Data is null) {
                        return new ResponseModel { Message = "Turma inválida" };
                    }

                    if (model.Dest_Professor_Id is null) {
                        return new ResponseModel { Message = "Turma inválida" };
                    }

                    CreateAulaRequest createAulaRequest = new() {
                        Turma_Id = ( int )model.Dest_Turma_Id,
                        Data = ( DateTime )model.Dest_Data,
                        Professor_Id = ( int )model.Dest_Professor_Id,
                        Observacao = "",
                    };

                    ResponseModel createAulaResponse = _aulaService.Insert(createAulaRequest);

                    if (createAulaResponse.Success == false) {
                        return createAulaResponse;
                    }

                    int createdAulaId = createAulaResponse.Object!.Id;

                    registroDest = new() {
                        Turma_Aula_Id = createdAulaId,
                        Aluno_Id = model.Aluno_Id,
                        Presente = null,
                        Reposicao = true,
                    };
                }

                if (registroDest is null) {
                    return new ResponseModel { Message = "Ocorreu algum erro ao coletar o registro destino do aluno" };
                }

                _db.TurmaAulaAlunos.Remove(registroSource);
                _db.TurmaAulaAlunos.Add(registroDest);
                _db.SaveChanges();

                response.Success = true;
                response.Message = "Reposição marcada com sucesso";
                response.Object = _db.CalendarioAlunoList.FirstOrDefault(a => a.Id == registroDest.Id);
            } catch (Exception ex) {
                response.Message = "Falha ao inserir reposição de aula do aluno: " + ex.ToString();
            }

            return response;
        }

        //    public async Task<ResponseModel> UploadImage(int alunoId, byte[] BinaryImage)
        //    {
        //        ResponseModel response = new() { Success = false };
        //        Console.WriteLine("Entrei no service");
        //        try {
        //            Aluno? aluno = _db.Alunos.Find(alunoId);

        //            if (aluno is null) {
        //                return new ResponseModel { Message = "Aluno não encontrado" };
        //            }

        //            var imagesFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Images");
        //            Directory.CreateDirectory(imagesFolder); // Garante que a pasta existe

        //            string fileName = @$"aluno{alunoId}_{TimeFunctions.HoraAtualBR():yyyy_dd_M_HH_mm_ss}";
        //            var filePath = Path.Combine(imagesFolder, fileName);

        //            Image image = new() {
        //                Name = filePath,
        //                Path = $"/Images/{fileName}"
        //            };

        //            // Keyword 'using' should automatically release resources when it is done
        //            using var stream = new FileStream(filePath, FileMode.Create);
        //            stream.Write(BinaryImage);

        //            aluno.Aluno_Foto = image.Path;

        //            _db.Alunos.Update(aluno);
        //            await _db.SaveChangesAsync();

        //            response.Success = true;
        //            response.Object = aluno;
        //            response.Message = "Foto do aluno resgatada com sucesso";
        //        } catch (Exception ex) {
        //            response.Message = "Falha no upload de imagem do aluno: " + ex.ToString();
        //        }

        //        return response;
        //    }
        //}
    }
}
