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

    //bool HasOccupyingTurmaInInterval(int salaId, DateTime start, DateTime end, int? ignoredTurmaId = null);
    //bool HasOccupyingAulaInInterval(int salaId, DateTime start, DateTime end, int? ignoredAulaId = null);
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
        List<Sala> salas = _db.Sala
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
            bool SalaAlreadyExists = _db.Sala.Any(s => s.NumeroSala == model.NumeroSala);

            if (SalaAlreadyExists) {
                return new ResponseModel { Message = "A operação foi cancelada pois existe outra sala com esse mesmo número" };
            }

            Sala newSala = new() {
                Andar = model.Andar,
                NumeroSala = model.NumeroSala,
            };

            _db.Sala.Add(newSala);
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
            Sala? sala = _db.Sala.Find(model.Id);

            if (sala is null) {
                return new ResponseModel { Message = "Sala não encontrada" };
            }

            // Não devo poder atualizar da sala para um outro numero que ja existe
            bool SalaAlreadyExists = _db.Sala.Any(s => s.NumeroSala == model.NumeroSala && s.Id != model.Id);

            if (SalaAlreadyExists) {
                return new ResponseModel { Message = "A operação foi cancelada pois existe outra sala com esse mesmo número" };
            }

            sala.Andar = model.Andar;
            sala.NumeroSala = model.NumeroSala;

            _db.Sala.Update(sala);
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
            Sala? sala = _db.Sala.Find(salaId);

            if (sala is null) {
                return new ResponseModel { Message = "Sala não encontrada" };
            }

            _db.Sala.Remove(sala);
            _db.SaveChanges();

            response.Success = true;
            response.Message = "Sala removida com sucesso";
        } catch (Exception ex) {
            response.Message = "Falha ao remover sala: " + ex.ToString();
        }

        return response;
    }

    //public bool HasOccupyingTurmaInInterval(int salaId, DateTime start, DateTime end, int? ignoredTurmaId = null)
    //{
    //    try {
    //        Sala sala = _db.Sala.Find(salaId) ?? throw new Exception("HasOccupyingTurmaInInterval: Sala não encontrada");

    //        TimeSpan twoHourInterval = TimeSpan.FromHours(2);

    //        if (start.Date != end.Date) {
    //            throw new Exception("HasOccupyingTurmaInInterval: Data de inicio e fim devem ser a mesma");
    //        }

    //        var turmasInSala = _db.Turma
    //        .Where(t =>
    //            t.Deactivated == null &&
    //            t.Sala_Id == salaId
    //        );

    //        bool hasTurmaInInterval = _db.Turma
    //        .Where(t =>
    //            t.Deactivated == null &&
    //            t.Sala_Id == salaId
    //        )
    //        .AsEnumerable() // Termina a query no banco, passando a responsabilidade do Any para o C#, queries do banco não lidam bem com TimeSpan
    //        .Any(a =>
    //            a.Id != IgnoredAulaId && // Se estou reagendando uma aula, não devo considerar ela mesma como conflito
    //            date.Day == a.Data.Day &&
    //            date.TimeOfDay > a.Data.TimeOfDay - twoHourInterval &&
    //            date.TimeOfDay < a.Data.TimeOfDay + twoHourInterval
    //        );

    //        return hasAulaConflict;
    //    } catch (Exception ex) {
    //        throw new Exception("Falha ao resgatar conflitos de aula do professor | " + ex.ToString());
    //    }
    //}

    //public bool HasOccupyingAulaInInterval(DateTime start, DateTime end, int? ignoredAulaId = null)
    //{
    //    throw new NotImplementedException();
    //}
}
