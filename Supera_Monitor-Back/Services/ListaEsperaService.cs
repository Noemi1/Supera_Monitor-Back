using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.ListaEspera;

namespace Supera_Monitor_Back.Services {
    public interface IListaEsperaService {
        ResponseModel Insert(CreateEsperaRequest model);
        List<AulaEsperaList> GetAllByAulaId(int aulaId);
        ResponseModel Remove(int listaEsperaId);
        ResponseModel Promote(int listaEsperaId);
    }

    public class ListaEsperaService : IListaEsperaService {
        private readonly DataContext _db;
        private readonly IMapper _mapper;
        private readonly Account? _account;

        public ListaEsperaService(DataContext db, IMapper mapper, IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _mapper = mapper;
            _account = ( Account? )httpContextAccessor.HttpContext?.Items["Account"];
        }

        public List<AulaEsperaList> GetAllByAulaId(int aulaId)
        {
            List<AulaEsperaList> listaEspera = _db.AulaEsperaList.Where(x => x.Aula_Id == aulaId).ToList();

            return listaEspera;
        }

        public ResponseModel Insert(CreateEsperaRequest model)
        {
            ResponseModel response = new() { Success = false };

            try {
                Aula? aula = _db.Aula.AsNoTracking().FirstOrDefault(a => a.Id == model.Aula_Id);

                if (aula is null) {
                    return new ResponseModel { Message = "Aula não foi encontrada" };
                }

                Aluno? aluno = _db.Aluno.FirstOrDefault(a => a.Id == model.Aluno_Id);

                if (aluno is null) {
                    return new ResponseModel { Message = "Aluno não foi encontrado" };
                }

                // Se o aluno já possui um registro na aula, não faz sentido que ele entre na lista de espera
                bool registroAlreadyExists = _db.Aula_Aluno.AsNoTracking().Any(r =>
                    r.Aula_Id == aula.Id &&
                    r.Aluno_Id == aluno.Id);

                if (registroAlreadyExists == true) {
                    return new ResponseModel { Message = "A operação não foi concluída porque o aluno em questão já está registrado na aula" };
                }

                // Se o aluno ja está na lista de espera, não faz sentido adicioná-lo novamente
                bool esperaAlreadyExists = _db.Aula_ListaEspera.AsNoTracking().Any(x =>
                    x.Aula_Id == aula.Id &&
                    x.Aluno_Id == aluno.Id
                );

                if (esperaAlreadyExists == true) {
                    return new ResponseModel { Message = "A operação não foi concluída porque o aluno em questão já está na lista de espera" };
                }

                Aula_ListaEspera newEspera = new() {
                    Aluno_Id = model.Aluno_Id,
                    Aula_Id = model.Aula_Id,
                    Created = TimeFunctions.HoraAtualBR(),
                    Account_Created_Id = _account.Id,
                };

                _db.Aula_ListaEspera.Add(newEspera);
                _db.SaveChanges();

                response.Success = true;
                response.Message = "Registro do aluno na lista de espera foi inserido com sucesso";
                response.Object = _db.AulaEsperaList.AsNoTracking().FirstOrDefault(x => x.Id == newEspera.Id);
            } catch (Exception ex) {
                response.Message = "Falha ao inserir aluno na lista de espera: " + ex.ToString();
            }
            return response;
        }

        public ResponseModel Promote(int listaEsperaId)
        {
            ResponseModel response = new() { Success = false };

            try {
                Aula_ListaEspera? espera = _db.Aula_ListaEspera.Find(listaEsperaId);

                if (espera is null) {
                    return new ResponseModel { Message = "Não foi encontrado o registro na lista de espera" };
                }

                Aula? aulaDestino = _db.Aula.AsNoTracking().FirstOrDefault(a => a.Id == espera.Aula_Id);

                if (aulaDestino is null) {
                    return new ResponseModel { Message = "Este registro de espera está apontando para uma aula que não existe" };
                }

                Aula? aulaLastRegistro = _db.Aula
                    .AsNoTracking()
                    .OrderByDescending(a => a.Data)
                    .FirstOrDefault();

                Aula_Aluno? lastRegistro = null;

                if (aulaLastRegistro != null) {
                    lastRegistro = _db.Aula_Aluno.AsNoTracking().FirstOrDefault(r =>
                        r.Aluno_Id == espera.Aluno_Id &&
                        r.Aula_Id == aulaLastRegistro.Id);
                }

                Aula_Aluno promotedRegistro = new() {
                    Aluno_Id = espera.Aluno_Id,
                    Aula_Id = espera.Aula_Id,

                    // O registro do aluno deve continuar de onde parou, TODO: Se não tem ultimo registro, buscar o início das apostilas do aluno
                    Apostila_AH_Id = lastRegistro?.Apostila_AH_Id,
                    NumeroPaginaAH = lastRegistro?.NumeroPaginaAH,
                    Apostila_Abaco_Id = lastRegistro?.Apostila_Abaco_Id,
                    NumeroPaginaAbaco = lastRegistro?.NumeroPaginaAbaco,
                };

                // Deve-se verificar se a aula tem espaço antes de promover o registro
                // Se a aula está associada a uma Turma, possui capacidade máxima e devemos verificar
                // Se a aula é independente, não há capacidade máxima, continuar sem problemas
                Turma? turma = _db.Turma.AsNoTracking().FirstOrDefault(t => t.Id == aulaDestino.Turma_Id);
                int registrosInAula = _db.Aula_Aluno.AsNoTracking().Count(r => r.Aula_Id == aulaDestino.Id);

                if (turma is not null) {
                    if (registrosInAula >= turma.CapacidadeMaximaAlunos) {
                        return new ResponseModel { Message = "Não é possível promover o aluno para a aula, a aula já está em capacidade máxima" };
                    }
                }

                _db.Aula_ListaEspera.Remove(espera);
                _db.Aula_Aluno.Add(promotedRegistro);

                _db.SaveChanges();

                response.Success = true;
                response.Message = "Registro do aluno foi promovido para a aula com sucesso";
                response.Object = _db.CalendarioAlunoList.AsNoTracking().FirstOrDefault(a => a.Id == promotedRegistro.Id);
            } catch (Exception ex) {
                response.Message = "Falha ao promover aluno da lista de espera para a aula: " + ex.ToString();
            }
            return response;
        }

        public ResponseModel Remove(int listaEsperaId)
        {
            ResponseModel response = new() { Success = false };

            try {
                Aula_ListaEspera? espera = _db.Aula_ListaEspera.Find(listaEsperaId);

                if (espera is null) {
                    return new ResponseModel { Message = "Não foi encontrado o registro na lista de espera" };
                }

                Aula? aulaDestino = _db.Aula.AsNoTracking().FirstOrDefault(a => a.Id == espera.Aula_Id);

                if (aulaDestino is null) {
                    return new ResponseModel { Message = "Este registro de espera está apontando para uma aula que não existe" };
                }

                _db.Aula_ListaEspera.Remove(espera);
                _db.SaveChanges();

                response.Success = true;
                response.Message = "Aluno foi removido da lista de espera com sucesso";
            } catch (Exception ex) {
                response.Message = "Falha ao remover aluno da lista de espera: " + ex.ToString();
            }
            return response;
        }
    }
}
