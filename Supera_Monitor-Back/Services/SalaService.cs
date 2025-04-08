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

    bool IsSalaOccupied(int Sala_Id, DateTime date, int duracaoMinutos, int? ignoredEventoId);
}


public class SalaService : ISalaService {
    private readonly DataContext _db;
    private readonly IMapper _mapper;

    public SalaService(DataContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public List<SalaModel> GetAllSalas()
    {
        List<Sala> salas = _db.Salas
            .OrderBy(s => s.Andar)
            .ThenBy(s => s.NumeroSala)
            .ToList();

        return _mapper.Map<List<SalaModel>>(salas);
    }

    public ResponseModel Insert(CreateSalaRequest model)
    {
        ResponseModel response = new() { Success = false };

        try {
            // Não devo poder criar duas salas com o mesmo numero
            bool SalaAlreadyExists = _db.Salas.Any(s => s.NumeroSala == model.NumeroSala);

            if (SalaAlreadyExists) {
                return new ResponseModel { Message = "A operação foi cancelada pois existe outra sala com esse mesmo número" };
            }

            Sala newSala = new() {
                Andar = model.Andar,
                NumeroSala = model.NumeroSala,
            };

            _db.Salas.Add(newSala);
            _db.SaveChanges();


            response.Success = true;
            response.Message = "Sala criada com sucesso";
            response.Object = _mapper.Map<SalaModel>(newSala);
        } catch (Exception ex) {
            response.Message = "Falha ao inserir nova sala: " + ex.ToString();
        }

        return response;
    }

    public ResponseModel Update(UpdateSalaRequest model)
    {
        ResponseModel response = new() { Success = false };

        try {
            // Não devo poder atualizar uma sala que não existe
            Sala? sala = _db.Salas.Find(model.Id);

            if (sala is null) {
                return new ResponseModel { Message = "Sala não encontrada" };
            }

            // Não devo poder atualizar da sala para um outro numero que ja existe
            bool SalaAlreadyExists = _db.Salas.Any(s => s.NumeroSala == model.NumeroSala && s.Id != model.Id);

            if (SalaAlreadyExists) {
                return new ResponseModel { Message = "A operação foi cancelada pois existe outra sala com esse mesmo número" };
            }

            sala.Andar = model.Andar;
            sala.NumeroSala = model.NumeroSala;

            _db.Salas.Update(sala);
            _db.SaveChanges();

            response.Success = true;
            response.Message = "Sala atualizada com sucesso";
            response.Object = _mapper.Map<SalaModel>(sala);
        } catch (Exception ex) {
            response.Message = "Falha ao atualizar sala: " + ex.ToString();
        }

        return response;
    }

    public ResponseModel Delete(int salaId)
    {
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
        } catch (Exception ex) {
            response.Message = "Falha ao remover sala: " + ex.ToString();
        }

        return response;
    }

    public bool IsSalaOccupied(int Sala_Id, DateTime date, int duracaoMinutos, int? ignoredEventoId)
    {
        var novoEventoInicio = date;
        var novoEventoFim = date.AddMinutes(duracaoMinutos);

        bool isSalaOccupied = _db.Eventos.Any(e =>
            e.Id != ignoredEventoId
            && e.Id != ( int )SalasIgnoradas.SALA_ONLINE
            && e.Id != ( int )SalasIgnoradas.SALA_DOS_PROFESSORES
            && e.Deactivated == null
            && e.Sala_Id == Sala_Id
            && e.Data < novoEventoFim // O evento existente começa antes do fim do novo evento
            && e.Data.AddMinutes(e.DuracaoMinutos) > novoEventoInicio // O evento existente termina depois do início do novo evento
        );

        return isSalaOccupied;
    }

    enum SalasIgnoradas {
        SALA_ONLINE = 13,
        SALA_DOS_PROFESSORES = 14
    }
}
