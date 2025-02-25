using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Turma;

namespace Supera_Monitor_Back.Services {
    public interface ITurmaService {
        TurmaList Get(int turmaId);
        ResponseModel Insert(CreateTurmaRequest model);
        ResponseModel Update(UpdateTurmaRequest model);
        ResponseModel Delete(int turmaId);
        ResponseModel ToggleDeactivate(int turmaId, string ipAddress);

        List<TurmaList> GetAll();

        List<PerfilCognitivoModel> GetAllPerfisCognitivos();
        List<AlunoList> GetAllAlunosByTurma(int turmaId);
    }

    public class TurmaService : ITurmaService {
        private readonly DataContext _db;
        private readonly IMapper _mapper;
        private readonly Account? _account;
        private readonly IProfessorService _professorService;

        public TurmaService(DataContext db, IMapper mapper, IHttpContextAccessor httpContextAccessor, IProfessorService professorService)
        {
            _db = db;
            _mapper = mapper;
            _account = ( Account? )httpContextAccessor.HttpContext?.Items["Account"];
            _professorService = professorService;
        }

        public TurmaList Get(int turmaId)
        {
            TurmaList? turma = _db.TurmaList.AsNoTracking().FirstOrDefault(t => t.Id == turmaId);

            if (turma == null) {
                throw new Exception("Turma não encontrada.");
            }

            return turma;
        }

        public List<TurmaList> GetAll()
        {
            List<TurmaList> turmas = _db.TurmaList.OrderBy(t => t.Nome).ToList();

            return turmas;
        }

        public List<PerfilCognitivoModel> GetAllPerfisCognitivos()
        {
            List<PerfilCognitivo> profiles = _db.PerfilCognitivo.ToList();

            return _mapper.Map<List<PerfilCognitivoModel>>(profiles);
        }

        public ResponseModel Insert(CreateTurmaRequest model)
        {
            ResponseModel response = new() { Success = false };

            try {
                // Não devo poder criar turma com um perfil cognitivo que não existe
                bool perfilCognitivoExists = _db.PerfilCognitivo.Any(p => p.Id == model.PerfilCognitivo_Id);

                if (!perfilCognitivoExists) {
                    return new ResponseModel { Message = "O perfil cognitivo informado na requisição não existe." };
                }

                // Não devo poder criar turma com um professor que não existe
                Professor? professor = _db.Professor
                    .Include(p => p.Account)
                    .FirstOrDefault(p => p.Id == model.Professor_Id);

                if (professor == null) {
                    return new ResponseModel { Message = "Professor não encontrado" };
                }

                // Não devo poder criar turma com um professor desativado
                if (professor.Account.Deactivated != null) {
                    return new ResponseModel { Message = "Não é possível criar uma turma com um professor desativado." };
                }

                // Se for passado um horário na requisição
                // Não devo permitir a criação de turma com um professor que já está ocupado nesse dia da semana / horário
                if (model.Horario.HasValue) {
                    bool isProfessorAvailable = _professorService.HasTurmaTimeConflict(
                        professorId: professor.Id,
                        DiaSemana: model.DiaSemana,
                        Horario: model.Horario.Value,
                        IgnoredTurmaId: null);

                    if (!isProfessorAvailable) {
                        return new ResponseModel { Message = "Não foi possível criar a turma. O professor responsável já tem compromissos no horário indicado." };
                    }
                }

                // Se for passado um Sala_Id na requisição, não devo permitir a criação de uma turma em uma sala que não existe
                if (model.Sala_Id.HasValue) {
                    bool salaExists = _db.Sala.Any(s => s.Id == model.Sala_Id);

                    if (salaExists == false) {
                        return new ResponseModel { Message = "Sala não encontrada." };
                    }
                }

                // Validations passed

                Turma turma = new();

                _mapper.Map<CreateTurmaRequest, Turma>(model, turma);

                turma.Created = TimeFunctions.HoraAtualBR();
                turma.Account_Created_Id = _account.Id;

                _db.Turma.Add(turma);
                _db.SaveChanges();

                Turma_PerfilCognitivo_Rel perfilCognitivoRel = new() {
                    Turma_Id = turma.Id,
                    PerfilCognitivo_Id = model.PerfilCognitivo_Id,
                };

                _db.Turma_PerfilCognitivo_Rel.Add(perfilCognitivoRel);
                _db.SaveChanges();

                response.Success = true;
                response.Message = "Turma cadastrada com sucesso";
                response.Object = _db.TurmaList.FirstOrDefault(t => t.Id == turma.Id);
            } catch (Exception ex) {
                response.Message = "Falha ao inserir nova turma: " + ex.ToString();
            }

            return response;
        }

        public ResponseModel Update(UpdateTurmaRequest model)
        {
            ResponseModel response = new() { Success = false };

            try {
                Turma? turma = _db.Turma.Find(model.Id);

                // Não devo poder atualizar uma turma que não existe
                if (turma == null) {
                    return new ResponseModel { Message = "Turma não encontrada" };
                }

                // Não devo poder atualizar uma turma desativada
                if (turma.Deactivated.HasValue) {
                    return new ResponseModel { Message = "Não é possível atualizar uma turma desativada." };
                }

                // Não devo poder criar turma com um perfil cognitivo que não existe
                bool perfilCognitivoExists = _db.PerfilCognitivo.Any(p => p.Id == model.PerfilCognitivo_Id);

                if (!perfilCognitivoExists) {
                    return new ResponseModel { Message = "O perfil cognitivo informado na requisição não existe." };
                }

                // Não devo poder atualizar turma com um professor que não existe
                Professor? professor = _db.Professor
                    .Include(p => p.Account)
                    .FirstOrDefault(p => p.Id == model.Professor_Id);

                if (professor == null) {
                    return new ResponseModel { Message = "Professor não encontrado" };
                }

                // Não devo poder criar turma com um professor desativado
                if (professor.Account.Deactivated != null) {
                    return new ResponseModel { Message = "Não é possível criar uma turma com um professor desativado." };
                }

                // Se for passado um horário na requisição
                // Não devo permitir a atualização de turma com um professor que já está ocupado nesse dia da semana / horário
                if (model.Horario.HasValue) {
                    bool isProfessorAvailable = _professorService.HasTurmaTimeConflict(
                        professorId: professor.Id,
                        DiaSemana: model.DiaSemana,
                        Horario: model.Horario.Value,
                        IgnoredTurmaId: turma.Id);

                    if (!isProfessorAvailable) {
                        return new ResponseModel { Message = "Não foi possível criar a turma. O professor responsável já tem compromissos no horário indicado." };
                    }
                }

                // Validations passed

                response.OldObject = _db.TurmaList.FirstOrDefault(t => t.Id == model.Id);

                turma.Nome = model.Nome;
                turma.Horario = model.Horario;
                turma.DiaSemana = model.DiaSemana;
                turma.CapacidadeMaximaAlunos = model.CapacidadeMaximaAlunos;

                turma.Sala_Id = model.Sala_Id;
                turma.Unidade_Id = model.Unidade_Id;
                turma.Professor_Id = model.Professor_Id;

                turma.LastUpdated = TimeFunctions.HoraAtualBR();

                _db.Turma.Update(turma);

                // Por enquanto, uma turma só tem um perfil cognitivo, que pode ser atualizado
                Turma_PerfilCognitivo_Rel? perfilCognitivoRel = _db.Turma_PerfilCognitivo_Rel.FirstOrDefault(p => p.Turma_Id == turma.Id);

                // Se não tem perfil cognitivo, cria um novo, senão atualiza
                if (perfilCognitivoRel is null) {
                    _db.Turma_PerfilCognitivo_Rel.Add(new() {
                        Turma_Id = turma.Id,
                        PerfilCognitivo_Id = model.PerfilCognitivo_Id,
                    });
                } else {
                    perfilCognitivoRel.PerfilCognitivo_Id = model.PerfilCognitivo_Id;
                    _db.Turma_PerfilCognitivo_Rel.Update(perfilCognitivoRel);
                }

                _db.SaveChanges();

                response.Success = true;
                response.Message = "Turma atualizada com sucesso";
                response.Object = _db.TurmaList.FirstOrDefault(t => t.Id == turma.Id);
            } catch (Exception ex) {
                response.Message = "Falha ao atualizar a turma: " + ex.ToString();
            }

            return response;
        }

        public ResponseModel Delete(int turmaId)
        {
            ResponseModel response = new() { Success = false };

            try {
                Turma? turma = _db.Turma.Find(turmaId);

                if (turma == null) {
                    return new ResponseModel { Message = "Turma não encontrada" };
                }

                // Validations passed

                response.Object = _db.TurmaList.FirstOrDefault(t => t.Id == turmaId);

                _db.Turma.Remove(turma);
                _db.SaveChanges();

                response.Message = "Turma excluída com sucesso";
                response.Success = true;
            } catch (Exception ex) {
                response.Message = "Falha ao excluir turma: " + ex.ToString();
            }

            return response;
        }

        public List<AlunoList> GetAllAlunosByTurma(int turmaId)
        {
            List<AlunoList> alunos = _db.AlunoList.Where(a => a.Turma_Id == turmaId).ToList();

            return alunos;
        }

        public ResponseModel ToggleDeactivate(int turmaId, string ipAddress)
        {
            Turma? turma = _db.Turma.Find(turmaId);

            if (turma == null) {
                return new ResponseModel { Message = "Turma não encontrada." };
            }

            if (_account == null) {
                return new ResponseModel { Message = "Não foi possível completar a ação. Autenticação do autor não encontrada." };
            }

            // Validations passed

            bool IsTurmaActive = turma.Deactivated == null;

            turma.Deactivated = IsTurmaActive ? TimeFunctions.HoraAtualBR() : null;

            _db.Turma.Update(turma);
            _db.SaveChanges();

            return new ResponseModel {
                Success = true,
                Object = _db.TurmaList.AsNoTracking().FirstOrDefault(t => t.Id == turma.Id),
            };
        }
    }
}
