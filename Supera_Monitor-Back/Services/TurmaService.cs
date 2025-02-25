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
                // Não devo poder criar turma com um tipo que não existe
                //bool TurmaTipoExists = _db.TurmaTipos.Any(t => t.Id == model.Turma_Tipo_Id);

                //if (!TurmaTipoExists) {
                //    return new ResponseModel { Message = "Este tipo de turma não existe." };
                //}

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

                TimeSpan twoHourInterval = TimeSpan.FromHours(2);

                // Não devo criar uma turma com um professor que já está ocupado nesse dia da semana / horário
                bool professorHasTimeConflicts = _db.Turma
                .Where(t =>
                    t.Deactivated == null &&
                    t.Professor_Id == professor.Id &&
                    t.DiaSemana == model.DiaSemana
                )
                .AsEnumerable() // Termina a query no banco, passando a responsabilidade do Any para o C#, queries do banco não lidam bem com TimeSpan
                .Any(t =>
                    model.Horario > t.Horario - twoHourInterval &&
                    model.Horario < t.Horario + twoHourInterval
                );

                if (professorHasTimeConflicts) {
                    return new ResponseModel { Message = "Não foi possível criar a turma. O professor responsável já tem compromissos no horário indicado." };
                }

                // Validations passed

                Turma turma = new();

                _mapper.Map<CreateTurmaRequest, Turma>(model, turma);

                turma.Created = TimeFunctions.HoraAtualBR();
                turma.Account_Created_Id = _account.Id;

                _db.Turma.Add(turma);
                _db.SaveChanges();

                response.Message = "Turma cadastrada com sucesso";
                response.Object = _db.TurmaList.FirstOrDefault(t => t.Id == turma.Id);
                response.Success = true;
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

                // Não devo poder atualizar turma com um tipo que não existe
                //bool TurmaTipoExists = _db.TurmaTipos.Any(t => t.Id == model.Turma_Tipo_Id);

                //if (!TurmaTipoExists) {
                //    return new ResponseModel { Message = "Este tipo de turma não existe." };
                //}

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

                TimeSpan twoHourInterval = TimeSpan.FromHours(2);

                // Não devo criar uma turma com um professor que já está ocupado nesse dia da semana / horário
                bool professorHasTimeConflicts = _db.Turma
                .Where(t =>
                    t.Deactivated == null &&
                    t.Professor_Id == professor.Id &&
                    t.DiaSemana == model.DiaSemana
                )
                .AsEnumerable() // Termina a query no banco, passando a responsabilidade do Any para o C#, queries do banco não lidam bem com TimeSpan
                .Any(t =>
                    model.Horario > t.Horario - twoHourInterval &&
                    model.Horario < t.Horario + twoHourInterval
                );

                if (professorHasTimeConflicts) {
                    return new ResponseModel { Message = "Não foi possível atualizar a turma. O professor responsável já tem compromissos no horário indicado." };
                }

                // Validations passed

                response.OldObject = _db.TurmaList.FirstOrDefault(t => t.Id == model.Id);

                turma.Nome = model.Nome;
                turma.DiaSemana = model.DiaSemana;
                turma.Horario = model.Horario;
                turma.Professor_Id = model.Professor_Id;
                //turma.Turma_Tipo_Id = model.Turma_Tipo_Id;
                turma.LastUpdated = TimeFunctions.HoraAtualBR();
                turma.CapacidadeMaximaAlunos = model.CapacidadeMaximaAlunos;
                turma.Unidade_Id = model.Unidade_Id;

                _db.Turma.Update(turma);
                _db.SaveChanges();

                response.Message = "Turma atualizada com sucesso";
                response.Object = _db.TurmaList.FirstOrDefault(t => t.Id == turma.Id);
                response.Success = true;
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
