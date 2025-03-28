using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;

namespace Supera_Monitor_Back.Services {

    public interface ILogService {
        List<LogList> GetList(int AccountId);
        LogModel GetLogAcao(int Id);
        Log Log(string Acao, string Entidade, dynamic Objeto, int? Account_Id);
        void LogError(Exception ex, string local);
    }

    public class LogService : ILogService {
        private readonly DataContext _db;
        private readonly IMapper _mapper;

        public LogService(
            DataContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }


        public List<LogList> GetList(int AccountId)
        {
            List<LogList> list = _db.LogLists.Where(x => x.Account_Id == AccountId)/*.Take(50)*/
                .OrderByDescending(x => x.Date)
                .ToList();

            return list;
        }

        public LogModel GetLogAcao(int Id)
        {
            Log? entity = _db.Logs.Include(x => x.Account)/*.Include(x => x.Customer)*/.FirstOrDefault(x => x.Id == Id);

            if (entity == null) {
                throw new Exception("Log não encontrado");
            }

            LogModel model = _mapper.Map<LogModel>(entity);

            return model;
        }

        public Log Log(string Acao, string Entidade, dynamic Objeto, int? Account_Id)
        {
            Log log = new() {
                Action = Acao,
                Entity = Entidade,
                Account_Id = Account_Id.Value,
                Object = Newtonsoft.Json.JsonConvert.SerializeObject(Objeto),
                Date = TimeFunctions.HoraAtualBR(),
            };

            _db.Logs.Add(log);
            _db.SaveChanges();

            return log;
        }

        public void LogError(Exception ex, string local)
        {
            try {
                LogError logError = new() {
                    Date = TimeFunctions.HoraAtualBR(),
                    Local = local,
                    Message = ex.ToString()
                };

                _db.LogErrors.Add(logError);
                _db.SaveChanges();
            } catch {

            }
        }
    }
}
