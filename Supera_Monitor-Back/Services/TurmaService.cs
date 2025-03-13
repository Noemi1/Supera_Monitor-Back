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
            var turma = _db.TurmaList.AsNoTracking().FirstOrDefault(t => t.Id == turmaId);

            if (turma == null) {
                throw new Exception("Turma não encontrada.");
            }

            // Busca os perfis cognitivos necessários diretamente do banco
            var perfisDaTurma = _db.PerfilCognitivo
                .Where(p => _db.Turma_PerfilCognitivo_Rel
                    .Where(tp => tp.Turma_Id == turmaId)
                    .Select(tp => tp.PerfilCognitivo_Id)
                    .Contains(p.Id))
                .ToList();

            turma.PerfilCognitivo = _mapper.Map<List<PerfilCognitivoModel>>(perfisDaTurma);

            return turma;
        }

        public List<TurmaList> GetAll()
        {
            List<TurmaList> turmas = _db.TurmaList.OrderBy(t => t.Nome).ToList();

            // Obtém todos os perfis cognitivos do banco de dados
            var allPerfisCognitivos = _db.PerfilCognitivo.ToList();
            var perfisCognitivosMap = allPerfisCognitivos.ToDictionary(p => p.Id);

            // Para cada turma, busca e associa os perfis cognitivos correspondentes
            foreach (var turma in turmas) {
                var perfilIds = _db.Turma_PerfilCognitivo_Rel
                    .Where(tp => tp.Turma_Id == turma.Id)
                    .Select(tp => tp.PerfilCognitivo_Id)
                    .ToList();

                // Obtém os perfis correspondentes da turma
                var perfisDaTurma = perfilIds
                    .Select(id => perfisCognitivosMap.GetValueOrDefault(id))
                    .Where(p => p != null)
                    .ToList();

                turma.PerfilCognitivo = _mapper.Map<List<PerfilCognitivoModel>>(perfisDaTurma);
            }

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
                    bool professorHasTurmaTimeConflict = _professorService.HasTurmaTimeConflict(
                        professorId: professor.Id,
                        DiaSemana: model.DiaSemana,
                        Horario: model.Horario.Value,
                        IgnoredTurmaId: null);

                    // TODO: Se há uma aula nesse horário, não vai bloquear

                    if (professorHasTurmaTimeConflict) {
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

                // Não devo poder atualizar turma com um perfil cognitivo que não existe
                List<PerfilCognitivo> requestPerfis = _mapper.Map<List<PerfilCognitivo>>(model.PerfilCognitivo);
                List<int> perfilIds = requestPerfis.Select(p => p.Id).ToList();

                int perfilCognitivoCount = _db.PerfilCognitivo.Count(p => perfilIds.Contains(p.Id));

                if (requestPerfis.Count != perfilCognitivoCount) {
                    return new ResponseModel { Message = "Algum dos perfis cognitivos informados na requisição não existe." };
                }

                // Validations passed

                Turma turma = new();

                _mapper.Map<CreateTurmaRequest, Turma>(model, turma);

                turma.Created = TimeFunctions.HoraAtualBR();
                turma.Account_Created_Id = _account.Id;

                _db.Turma.Add(turma);
                _db.SaveChanges();

                foreach (PerfilCognitivo perfil in requestPerfis) {
                    Turma_PerfilCognitivo_Rel newTurmaPerfilRel = new() {
                        Turma_Id = turma.Id,
                        PerfilCognitivo_Id = perfil.Id,
                    };

                    _db.Turma_PerfilCognitivo_Rel.Add(newTurmaPerfilRel);
                }

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

                // Não devo poder atualizar turma com um professor que não existe
                Professor? professor = _db.Professor
                    .Include(p => p.Account)
                    .FirstOrDefault(p => p.Id == model.Professor_Id);

                if (professor == null) {
                    return new ResponseModel { Message = "Professor não encontrado" };
                }

                // Não devo poder criar turma com um professor desativado
                if (professor.Account.Deactivated != null) {
                    return new ResponseModel { Message = "Não é possível atualizar uma turma com um professor desativado." };
                }

                // Se for passado um horário na requisição
                // Não devo permitir a atualização de turma com um professor que já está ocupado nesse dia da semana / horário
                if (model.Horario.HasValue) {
                    bool professorHasTurmaTimeConflict = _professorService.HasTurmaTimeConflict(
                        professorId: professor.Id,
                        DiaSemana: model.DiaSemana,
                        Horario: model.Horario.Value,
                        IgnoredTurmaId: turma.Id);

                    if (professorHasTurmaTimeConflict) {
                        return new ResponseModel { Message = "Não foi possível atualizar a turma. O professor responsável já tem compromissos no horário indicado." };
                    }
                }

                // Não devo poder atualizar turma com um perfil cognitivo que não existe
                List<PerfilCognitivo> requestPerfis = _mapper.Map<List<PerfilCognitivo>>(model.PerfilCognitivo);
                List<int> perfilIds = requestPerfis.Select(p => p.Id).ToList();

                int perfilCognitivoCount = _db.PerfilCognitivo.Count(p => perfilIds.Contains(p.Id));

                if (requestPerfis.Count != perfilCognitivoCount) {
                    return new ResponseModel { Message = "Algum dos perfis cognitivos informados na requisição não existe." };
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
                _db.SaveChanges();

                // Remove os perfis cognitivos antigos e insere os novos (ineficiente, se os perfis não mudaram, ainda assim remove e adiciona)
                // se tiver dando problema, peço perdão e prometo que vou melhorar c: depois == nunca => true
                List<Turma_PerfilCognitivo_Rel> perfisCognitivos = _db.Turma_PerfilCognitivo_Rel.Where(p => p.Turma_Id == turma.Id).ToList();

                _db.Turma_PerfilCognitivo_Rel.RemoveRange(perfisCognitivos);
                _db.SaveChanges();

                foreach (PerfilCognitivo perfil in requestPerfis) {
                    Turma_PerfilCognitivo_Rel newTurmaPerfilRel = new() {
                        Turma_Id = turma.Id,
                        PerfilCognitivo_Id = perfil.Id,
                    };

                    _db.Turma_PerfilCognitivo_Rel.Add(newTurmaPerfilRel);
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
