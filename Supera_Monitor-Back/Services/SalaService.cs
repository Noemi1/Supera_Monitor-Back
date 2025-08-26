using AutoMapper;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Sala;

namespace Supera_Monitor_Back.Services;

public interface ISalaService {
    List<SalaModel> GetAllSalas();
    ResponseModel Insert(CreateSalaRequest model);
    ResponseModel Update(UpdateSalaRequest model);
    ResponseModel Delete(int salaId);

    bool Exists(int Sala_Id);
    bool IsSalaOccupied(int Sala_Id, DateTime date, int duracaoMinutos, int? ignoredEventoId);
    bool IsSalaRecurrentlyOccupied(int Sala_Id, int DiaSemana, TimeSpan Horario, int? IgnoredTurmaId, int DuracaoMinutos = 120);
}

public class SalaService : ISalaService {
    private readonly DataContext _db;
    private readonly IMapper _mapper;

    public SalaService(DataContext db, IMapper mapper) {
        _db = db;
        _mapper = mapper;
    }

    public List<SalaModel> GetAllSalas() {
        var salas = _db.Salas
            .OrderBy(s => s.Andar)
            .ThenBy(s => s.NumeroSala)
            .ToList();

        return _mapper.Map<List<SalaModel>>(salas);
    }

    public ResponseModel Insert(CreateSalaRequest model) {
        ResponseModel response = new() { Success = false };

        try {
            // Não devo poder criar duas salas com o mesmo numero
            bool salaAlreadyExists = _db.Salas.Any(s => s.NumeroSala == model.NumeroSala);

            if (salaAlreadyExists) {
                return new ResponseModel { Message = "A operação foi cancelada pois existe outra sala com esse mesmo número" };
            }

            if (string.IsNullOrEmpty(model.Descricao)) {
                return new ResponseModel { Message = "A sala precisa ter um nome/descrição" };
            }

            Sala newSala = new()
            {
                Andar = model.Andar,
                NumeroSala = model.NumeroSala,
                Descricao = model.Descricao,
                Online = model.Online ?? false,
            };

            _db.Salas.Add(newSala);
            _db.SaveChanges();


            response.Success = true;
            response.Message = "Sala criada com sucesso";
            response.Object = _mapper.Map<SalaModel>(newSala);
        }
        catch (Exception ex) {
            response.Message = $"Falha ao inserir nova sala: {ex}";
        }

        return response;
    }

    public ResponseModel Update(UpdateSalaRequest model) {
        ResponseModel response = new() { Success = false };

        try {
            // Não devo poder atualizar uma sala que não existe
            Sala? sala = _db.Salas.Find(model.Id);

            if (sala is null) {
                return new ResponseModel { Message = "Sala não encontrada" };
            }

            // Não devo poder atualizar da sala para um outro numero que ja existe
            bool salaAlreadyExists = _db.Salas.Any(s => s.NumeroSala == model.NumeroSala && s.Id != model.Id);

            if (salaAlreadyExists) {
                return new ResponseModel { Message = "A operação foi cancelada pois existe outra sala com esse mesmo número" };
            }

            if (string.IsNullOrEmpty(model.Descricao)) {
                return new ResponseModel { Message = "A sala precisa ter um nome/descrição" };
            }

            sala.Andar = model.Andar;
            sala.NumeroSala = model.NumeroSala;
            sala.Descricao = model.Descricao;

            _db.Salas.Update(sala);
            _db.SaveChanges();

            response.Success = true;
            response.Message = "Sala atualizada com sucesso";
            response.Object = _mapper.Map<SalaModel>(sala);
        }
        catch (Exception ex) {
            response.Message = $"Falha ao atualizar sala: {ex}";
        }

        return response;
    }

    public ResponseModel Delete(int salaId) {
        ResponseModel response = new() { Success = false };

        try {
            // Não devo poder deletar uma sala que não existe
            Sala? sala = _db.Salas.Find(salaId);

            if (sala is null) {
                return new ResponseModel { Message = "Sala não encontrada" };
            }

            _db.Salas.Remove(sala);
            _db.SaveChanges();

            response.Success = true;
            response.Message = "Sala removida com sucesso";
        }
        catch (Exception ex) {
            response.Message = $"Falha ao remover sala: {ex}";
        }

        return response;
    }

    public bool IsSalaOccupied(int Sala_Id, DateTime date, int duracaoMinutos, int? ignoredEventoId) {
        Sala? sala = _db.Salas.FirstOrDefault(s => s.Id == Sala_Id);

        if (sala is null) {
            return false;
        }

        if (sala.Online) {
            return false;
        }

        var novoEventoInicio = date;
        var novoEventoFim = date.AddMinutes(duracaoMinutos);

        bool isSalaOccupied = _db.Eventos.Any(e =>
            e.Id != ignoredEventoId
            && e.Deactivated == null
            && e.Sala_Id == Sala_Id
            && e.Data < novoEventoFim // O evento existente começa antes do fim do novo evento
            && e.Data.AddMinutes(e.DuracaoMinutos) > novoEventoInicio // O evento existente termina depois do início do novo evento
        );

        return isSalaOccupied;
    }

    public bool IsSalaRecurrentlyOccupied(int Sala_Id, int DiaSemana, TimeSpan Horario, int? IgnoredTurmaId, int DuracaoMinutos = 120) {
        Sala sala = _db.Salas.Find(Sala_Id) ?? throw new Exception("Sala não encontrada");

        if (sala.Online) {
            return false;
        }

        TimeSpan duracaoMinutos = TimeSpan.FromMinutes((int)DuracaoMinutos!);

        var activeTurmas = _db.Turmas.Where(t =>
            t.Deactivated == null &&
            t.Sala_Id == Sala_Id &&
            t.DiaSemana == DiaSemana &&
            (IgnoredTurmaId == null || t.Id != IgnoredTurmaId))
            .ToList();

        return FindRecurrentConflicts(Horario, duracaoMinutos, activeTurmas);
    }

    public static bool FindRecurrentConflicts(TimeSpan Horario, TimeSpan DuracaoMinutos, List<Turma> Turmas) {
        TimeSpan startEventTime = Horario.Subtract(DuracaoMinutos);
        TimeSpan endEventTime = Horario.Add(DuracaoMinutos);

        // Vejo uma possibilidade de bug em horários extremos ex.: Evento às 23hrs ou 01hrs
        // Clamp do horário se entrar em um desses casos
        if (startEventTime < TimeSpan.Zero) {
            startEventTime = TimeSpan.Zero;
        }

        if (endEventTime > TimeSpan.FromHours(24)) {
            endEventTime = TimeSpan.FromHours(24);
        }

        bool hasCollision = Turmas
            .Any(t =>
                t.Horario > startEventTime &&
                t.Horario < endEventTime);

        return hasCollision;
    }

    public bool Exists(int Sala_Id) {
        return _db.Salas.Any(s => s.Id == Sala_Id);
    }
}
