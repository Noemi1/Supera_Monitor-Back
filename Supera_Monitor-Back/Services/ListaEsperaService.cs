namespace Supera_Monitor_Back.Services {
    public interface IListaEsperaService {
        //ResponseModel Insert(CreateEsperaRequest model);
        //List<AulaEsperaList> GetAllByAulaId(int aulaId);
        //ResponseModel Remove(int listaEsperaId);
        //ResponseModel Promote(int listaEsperaId);
    }

    public class ListaEsperaService : IListaEsperaService {
        //    private readonly DataContext _db;
        //    private readonly Account? _account;

        //    public ListaEsperaService(DataContext db, IHttpContextAccessor httpContextAccessor) {
        //        _db = db;
        //        _account = (Account?)httpContextAccessor.HttpContext?.Items["Account"];
        //    }

        //    public List<AulaEsperaList> GetAllByAulaId(int aulaId) {
        //        List<AulaEsperaList> listaEspera = _db.AulaEsperaLists.Where(x => x.Aula_Id == aulaId).ToList();

        //        return listaEspera;
        //    }

        //    public ResponseModel Insert(CreateEsperaRequest model) {
        //        ResponseModel response = new() { Success = false };

        //        try {
        //            Evento? evento = _db.Eventos
        //                .Include(e => e.Evento_Aula)
        //                .Include(e => e.Evento_Participacao_Alunos)
        //                .FirstOrDefault(e => e.Id == model.Aula_Id);

        //            if (evento is null) {
        //                return new ResponseModel { Message = "Evento não foi encontrado" };
        //            }

        //            if (evento.Evento_Aula is null) {
        //                return new ResponseModel { Message = "Aula não foi encontrada" };
        //            }

        //            Aluno? aluno = _db.Alunos.FirstOrDefault(a => a.Id == model.Aluno_Id);

        //            if (aluno is null) {
        //                return new ResponseModel { Message = "Aluno não foi encontrado" };
        //            }

        //            // Se o aluno já possui um registro na aula, não faz sentido que ele entre na lista de espera
        //            bool registroAlreadyExists = evento.Evento_Participacao_Alunos.Any(p => p.Aluno_Id == aluno.Id);

        //            if (registroAlreadyExists == true) {
        //                return new ResponseModel { Message = "O aluno já está registrado na aula" };
        //            }

        //            // Se o aluno ja está na lista de espera, não faz sentido adicioná-lo novamente
        //            bool alreadyInEspera = _db.Aula_ListaEsperas.Any(e =>
        //                e.Aula_Id == evento.Id
        //                && e.Aluno_Id == aluno.Id);

        //            if (alreadyInEspera) {
        //                return new ResponseModel { Message = "O aluno em questão já está na lista de espera" };
        //            }

        //            Aula_ListaEspera newEspera = new()
        //            {
        //                Aluno_Id = model.Aluno_Id,
        //                Aula_Id = model.Aula_Id,
        //                Created = TimeFunctions.HoraAtualBR(),
        //                Account_Created_Id = _account.Id,
        //            };

        //            _db.Aula_ListaEsperas.Add(newEspera);
        //            _db.SaveChanges();

        //            response.Success = true;
        //            response.Message = "Aluno foi inserido na lista de espera com sucesso";
        //            response.Object = _db.AulaEsperaLists.AsNoTracking().FirstOrDefault(x => x.Id == newEspera.Id);
        //        }
        //        catch (Exception ex) {
        //            response.Message = $"Falha ao inserir aluno na lista de espera: {ex}";
        //        }

        //        return response;
        //    }

        //    public ResponseModel Promote(int listaEsperaId) {
        //        ResponseModel response = new() { Success = false };

        //        try {
        //            Aula_ListaEspera? espera = _db.Aula_ListaEsperas.Find(listaEsperaId);

        //            if (espera is null) {
        //                return new ResponseModel { Message = "Registro na lista de espera não encontrado" };
        //            }

        //            Evento? eventoDestino = _db.Eventos
        //                .Include(e => e.Evento_Aula)
        //                .Include(e => e.Evento_Participacao_Alunos)
        //                .FirstOrDefault(a => a.Id == espera.Aula_Id);

        //            if (eventoDestino is null) {
        //                return new ResponseModel { Message = "Evento não encontrado" };
        //            }

        //            if (eventoDestino.Evento_Aula == null) {
        //                return new ResponseModel { Message = "Aula não encontrada" };
        //            }

        //            // Deve-se verificar se a aula tem espaço antes de promover o registro
        //            int registrosInAula = eventoDestino.Evento_Participacao_Alunos.Count(p => p.Deactivated == null);

        //            if (registrosInAula >= eventoDestino.CapacidadeMaximaAlunos) {
        //                return new ResponseModel { Message = "Não é possível promover o aluno para a aula, a aula já está em capacidade máxima" };
        //            }

        //            Evento_Participacao_Aluno promotedRegistro = new()
        //            {
        //                Aluno_Id = espera.Aluno_Id,
        //                Evento_Id = espera.Aula_Id,
        //            };

        //            _db.Aula_ListaEsperas.Remove(espera);
        //            _db.Evento_Participacao_Alunos.Add(promotedRegistro);

        //            _db.SaveChanges();

        //            response.Success = true;
        //            response.Message = "Aluno foi promovido para a aula com sucesso";
        //            response.Object = _db.CalendarioAlunoLists.AsNoTracking().FirstOrDefault(a => a.Id == promotedRegistro.Id);
        //        }
        //        catch (Exception ex) {
        //            response.Message = $"Falha ao promover aluno da lista de espera para a aula: {ex}";
        //        }
        //        return response;
        //    }

        //    public ResponseModel Remove(int listaEsperaId) {
        //        ResponseModel response = new() { Success = false };

        //        try {
        //            Aula_ListaEspera? espera = _db.Aula_ListaEsperas.Find(listaEsperaId);

        //            if (espera is null) {
        //                return new ResponseModel { Message = "Não foi encontrado o registro na lista de espera" };
        //            }

        //            Evento? eventoDestino = _db.Eventos
        //                .Include(e => e.Evento_Aula)
        //                .FirstOrDefault(a => a.Id == espera.Aula_Id);

        //            if (eventoDestino is null) {
        //                return new ResponseModel { Message = "Evento não encontrado" };
        //            }

        //            if (eventoDestino.Evento_Aula == null) {
        //                return new ResponseModel { Message = "Aula não encontrada" };
        //            }

        //            _db.Aula_ListaEsperas.Remove(espera);
        //            _db.SaveChanges();

        //            response.Success = true;
        //            response.Message = "Aluno foi removido da lista de espera com sucesso";
        //        }
        //        catch (Exception ex) {
        //            response.Message = "Falha ao remover aluno da lista de espera: " + ex.ToString();
        //        }
        //        return response;
        //    }
    }
}
