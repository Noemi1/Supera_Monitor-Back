using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Turma;

namespace Supera_Monitor_Back.Services;

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
        TurmaList? turma = _db.TurmaLists.AsNoTracking().FirstOrDefault(t => t.Id == turmaId);

        if (turma == null) {
            throw new Exception("Turma não encontrada.");
        }

        List<Turma_PerfilCognitivo_Rel> turmaPerfisCognitivos = _db.Turma_PerfilCognitivo_Rels
            .Where(p => p.Turma_Id == turma.Id)
            .Include(p => p.PerfilCognitivo)
            .ToList();

        List<PerfilCognitivo> perfisCognitivos = turmaPerfisCognitivos.Select(p => p.PerfilCognitivo).ToList();

        turma.PerfilCognitivo = _mapper.Map<List<PerfilCognitivoModel>>(perfisCognitivos);

        return turma;
    }

    public List<TurmaList> GetAll()
    {
        List<TurmaList> turmas = _db.TurmaLists.OrderBy(t => t.Nome).ToList();

        // Obtém todos os perfis cognitivos do banco de dados
        var allPerfisCognitivos = _db.PerfilCognitivos.ToList();
        var perfisCognitivosMap = allPerfisCognitivos.ToDictionary(p => p.Id);

        // Para cada turma, busca e associa os perfis cognitivos correspondentes
        foreach (var turma in turmas) {
            var perfilIds = _db.Turma_PerfilCognitivo_Rels
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
        List<PerfilCognitivo> profiles = _db.PerfilCognitivos.ToList();

        return _mapper.Map<List<PerfilCognitivoModel>>(profiles);
    }

    public ResponseModel Insert(CreateTurmaRequest model)
    {
        ResponseModel response = new() { Success = false };

        try {
            // Não devo poder criar turma com um professor que não existe
            Professor? professor = _db.Professors
                .Include(p => p.Account)
                .FirstOrDefault(p => p.Id == model.Professor_Id);

            if (professor == null) {
                return new ResponseModel { Message = "Professor não encontrado" };
            }

            // Não devo poder criar turma com um professor desativado
            if (professor.Account.Deactivated != null) {
                return new ResponseModel { Message = "Não é possível criar uma turma com um professor desativado." };
            }

            // Não devo permitir a criação de turma com um professor que já está ocupado nesse dia da semana / horário
            // Não considera horário de eventos, apenas de turmas
            bool professorHasTurmaTimeConflict = _professorService.HasTurmaTimeConflict(
                professorId: professor.Id,
                DiaSemana: model.DiaSemana,
                Horario: model.Horario,
                IgnoredTurmaId: null);

            if (professorHasTurmaTimeConflict) {
                return new ResponseModel { Message = "Não foi possível criar a turma. O professor responsável já tem compromissos no horário indicado." };
            }

            // Não devo poder registrar uma aula em uma sala que não existe
            bool salaExists = _db.Salas.Any(s => s.Id == model.Sala_Id);

            if (!salaExists) {
                return new ResponseModel { Message = "Sala não encontrada" };
            }

            TimeSpan twoHours = TimeSpan.FromHours(2);

            // Não devo permitir que duas turmas usem a mesma sala recorrentemente no mesmo horário
            bool salaIsAlreadyOccupied = _db.Turmas
                .Where(t =>
                    t.Deactivated == null
                    && t.DiaSemana == model.DiaSemana
                    && t.Sala_Id == model.Sala_Id)
                .AsEnumerable()
                .Any(t => (( TimeSpan )t.Horario! - model.Horario).Duration() < TimeSpan.FromHours(2));

            if (salaIsAlreadyOccupied) {
                return new ResponseModel { Message = "Sala já está ocupada nesse horário" };
            }

            // Não devo poder criar turma com um perfil cognitivo que não existe
            List<int> perfisCognitivos = model.PerfilCognitivo;

            int validPerfisCognitivos = _db.PerfilCognitivos.Count(p => perfisCognitivos.Contains(p.Id));

            if (perfisCognitivos.Count != validPerfisCognitivos) {
                return new ResponseModel { Message = "Algum dos perfis cognitivos informados na requisição não existe." };
            }

            // Validations passed

            Turma turma = _mapper.Map<Turma>(model);

            turma.Created = TimeFunctions.HoraAtualBR();
            turma.Account_Created_Id = _account.Id;

            _db.Turmas.Add(turma);
            _db.SaveChanges();

            foreach (int perfilCognitivoId in perfisCognitivos) {
                Turma_PerfilCognitivo_Rel newTurmaPerfilCognitivoRel = new() {
                    Turma_Id = turma.Id,
                    PerfilCognitivo_Id = perfilCognitivoId
                };

                _db.Turma_PerfilCognitivo_Rels.Add(newTurmaPerfilCognitivoRel);
            }

            _db.SaveChanges();

            response.Success = true;
            response.Message = "Turma cadastrada com sucesso";
            response.Object = _db.TurmaLists.FirstOrDefault(t => t.Id == turma.Id);
        } catch (Exception ex) {
            response.Message = $"Falha ao inserir nova turma: {ex}";
        }

        return response;
    }

    public ResponseModel Update(UpdateTurmaRequest model)
    {
        ResponseModel response = new() { Success = false };

        try {
            Turma? turma = _db.Turmas.Find(model.Id);

            // Não devo poder atualizar uma turma que não existe
            if (turma == null) {
                return new ResponseModel { Message = "Turma não encontrada" };
            }

            // Não devo poder atualizar uma turma desativada
            if (turma.Deactivated.HasValue) {
                return new ResponseModel { Message = "Não é possível atualizar uma turma desativada." };
            }

            // Não devo poder atualizar turma com um professor que não existe
            Professor? professor = _db.Professors
                .Include(p => p.Account)
                .FirstOrDefault(p => p.Id == model.Professor_Id);

            if (professor == null) {
                return new ResponseModel { Message = "Professor não encontrado" };
            }

            // Não devo poder criar turma com um professor desativado
            if (professor.Account.Deactivated != null) {
                return new ResponseModel { Message = "Não é possível atualizar uma turma com um professor desativado." };
            }

            // Não devo permitir a atualização de turma com um professor que já está ocupado nesse dia da semana / horário
            bool professorHasTurmaTimeConflict = _professorService.HasTurmaTimeConflict(
                professorId: professor.Id,
                DiaSemana: model.DiaSemana,
                Horario: model.Horario,
                IgnoredTurmaId: turma.Id);

            if (professorHasTurmaTimeConflict) {
                return new ResponseModel { Message = "Não foi possível atualizar a turma. O professor responsável já tem compromissos no horário indicado." };
            }

            // Não devo permitir a atualização de uma turma em uma sala que não existe
            bool salaExists = _db.Salas.Any(s => s.Id == model.Sala_Id);

            if (salaExists == false) {
                return new ResponseModel { Message = "Sala não encontrada." };
            }

            // Não devo permitir que duas turmas usem a mesma sala recorrentemente no mesmo horário
            bool salaIsAlreadyOccupied = _db.Turmas
                .Where(t =>
                    t.Id != model.Id
                    && t.Deactivated == null
                    && t.DiaSemana == model.DiaSemana
                    && t.Sala_Id == model.Sala_Id)
                .AsEnumerable()
                .Any(t => (( TimeSpan )t.Horario! - model.Horario).Duration() < TimeSpan.FromHours(2));

            if (salaIsAlreadyOccupied) {
                return new ResponseModel { Message = "Sala já está ocupada nesse horário" };
            }

            // Não devo poder atualizar turma com um perfil cognitivo que não existe
            List<int> perfisCognitivos = model.PerfilCognitivo;

            int validPerfisCognitivos = _db.PerfilCognitivos.Count(p => perfisCognitivos.Contains(p.Id));

            if (perfisCognitivos.Count != validPerfisCognitivos) {
                return new ResponseModel { Message = "Algum dos perfis cognitivos informados na requisição não existe." };
            }

            // Validations passed

            var oldObject = _db.TurmaLists.FirstOrDefault(t => t.Id == model.Id);

            turma.Nome = model.Nome;
            turma.Horario = model.Horario;
            turma.DiaSemana = model.DiaSemana;
            turma.CapacidadeMaximaAlunos = model.CapacidadeMaximaAlunos;

            turma.Sala_Id = model.Sala_Id;
            turma.Unidade_Id = model.Unidade_Id;
            turma.Professor_Id = model.Professor_Id;

            turma.LastUpdated = TimeFunctions.HoraAtualBR();

            _db.Turmas.Update(turma);
            _db.SaveChanges();

            // Remove os perfis cognitivos antigos e insere os novos (ineficiente, se os perfis não mudaram, ainda assim remove e adiciona)
            // se tiver dando problema, peço perdão e prometo que vou melhorar c: depois == nunca => true
            List<Turma_PerfilCognitivo_Rel> turmaPerfisCognitivos = _db.Turma_PerfilCognitivo_Rels
                .Where(p => p.Turma_Id == turma.Id)
                .Include(p => p.PerfilCognitivo)
                .ToList();

            _db.Turma_PerfilCognitivo_Rels.RemoveRange(turmaPerfisCognitivos);
            _db.SaveChanges();

            foreach (int perfilCognitivoId in perfisCognitivos) {
                Turma_PerfilCognitivo_Rel newTurmaPerfilRel = new() {
                    Turma_Id = turma.Id,
                    PerfilCognitivo_Id = perfilCognitivoId,
                };

                _db.Turma_PerfilCognitivo_Rels.Add(newTurmaPerfilRel);
            }

            _db.SaveChanges();

            List<PerfilCognitivo> turmaPerfilCognitivo = turmaPerfisCognitivos
                .Select(p => p.PerfilCognitivo)
                .ToList();

            TurmaList newObject = _db.TurmaLists.First(t => t.Id == turma.Id);

            newObject.PerfilCognitivo = _mapper.Map<List<PerfilCognitivoModel>>(turmaPerfilCognitivo);

            response.Success = true;
            response.Message = "Turma atualizada com sucesso";
            response.Object = newObject;
            response.OldObject = oldObject;
        } catch (Exception ex) {
            response.Message = $"Falha ao atualizar a turma: {ex}";
        }

        return response;
    }

    public ResponseModel Delete(int turmaId)
    {
        ResponseModel response = new() { Success = false };

        try {
            Turma? turma = _db.Turmas.Find(turmaId);

            if (turma == null) {
                return new ResponseModel { Message = "Turma não encontrada" };
            }

            // Validations passed

            response.Object = _db.TurmaLists.AsNoTracking().FirstOrDefault(t => t.Id == turmaId);

            var turmaPerfisCognitivos = _db.Turma_PerfilCognitivo_Rels.Where(p => p.Turma_Id == turmaId);

            _db.Turma_PerfilCognitivo_Rels.RemoveRange(turmaPerfisCognitivos);
            _db.SaveChanges();

            _db.Turmas.Remove(turma);
            _db.SaveChanges();

            response.Message = "Turma excluída com sucesso";
            response.Success = true;
        } catch (Exception ex) {
            response.Message = $"Falha ao excluir turma: {ex}";
        }

        return response;
    }

    public List<AlunoList> GetAllAlunosByTurma(int turmaId)
    {
        List<AlunoList> alunos = _db.AlunoLists.Where(a => a.Turma_Id == turmaId).ToList();

        return alunos;
    }

    public ResponseModel ToggleDeactivate(int turmaId, string ipAddress)
    {
        ResponseModel response = new() { Success = false };

        try {
            Turma? turma = _db.Turmas.Find(turmaId);

            if (turma == null) {
                return new ResponseModel { Message = "Turma não encontrada." };
            }

            if (_account == null) {
                return new ResponseModel { Message = "Não foi possível completar a ação. Autenticação do autor não encontrada." };
            }

            // Validations passed

            turma.Deactivated = turma.Deactivated.HasValue ? null : TimeFunctions.HoraAtualBR();
            string actionResult = turma.Deactivated.HasValue ? "desativada" : "reativada";

            _db.Turmas.Update(turma);
            _db.SaveChanges();

            response.Success = true;
            response.Object = _db.TurmaLists.AsNoTracking().FirstOrDefault(t => t.Id == turma.Id);
            response.Message = $"Turma {actionResult} com sucesso.";
        } catch (Exception ex) {
            response.Message = $"Falha ao desativar turma: {ex}";
        }

        return response;
    }
}
