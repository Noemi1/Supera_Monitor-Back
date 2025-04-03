using AutoMapper;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Accounts;
using Supera_Monitor_Back.Models.Professor;

namespace Supera_Monitor_Back.Services {
    public interface IProfessorService {
        ProfessorList Get(int professorId);
        ResponseModel Insert(CreateProfessorRequest model, string ipAddress);
        ResponseModel Update(UpdateProfessorRequest model);
        ResponseModel Delete(int professorId);

        List<ProfessorList> GetAll();

        List<KitResponse> GetAllKits();
        List<ApostilaList> GetAllApostilas();
        List<NivelCertificacaoModel> GetAllCertificacoes();

        bool HasTurmaTimeConflict(int professorId, int DiaSemana, TimeSpan Horario, int? IgnoredTurmaId);
        bool HasEventoParticipacaoConflict(int professorId, DateTime Data, int DuracaoMinutos, int? IgnoredEventoId);
    }

    public class ProfessorService : IProfessorService {
        private readonly DataContext _db;
        private readonly IMapper _mapper;
        private readonly IUserService _userService;

        public ProfessorService(DataContext db, IMapper mapper, IUserService userService)
        {
            _db = db;
            _mapper = mapper;
            _userService = userService;
        }

        public ProfessorList Get(int professorId)
        {
            ProfessorList? professor = _db.ProfessorLists.FirstOrDefault(p => p.Id == professorId);

            if (professor == null) {
                throw new Exception("Professor não encontrado.");
            }


            return professor;
        }

        public List<ProfessorList> GetAll()
        {
            List<ProfessorList> professores = _db.ProfessorLists.OrderBy(p => p.Nome).ToList();

            return professores;
        }

        public ResponseModel Insert(CreateProfessorRequest model, string ipAddress)
        {
            ResponseModel response = new() { Success = false };

            try {
                Professor professor = new() {
                    DataInicio = model.DataInicio,
                    CorLegenda = model.CorLegenda,
                    DataNascimento = model.DataNascimento,
                };

                // Só atribuir o nivel de certificacao passado na requisição se este existir, caso contrário, nulo
                bool NivelCertificacaoExists = _db.Professor_NivelCertificacaos.Any(n => n.Id == model.Professor_NivelCertificacao_Id);

                professor.Professor_NivelCertificacao_Id = NivelCertificacaoExists ? model.Professor_NivelCertificacao_Id : null;

                // Se for passado um Account_Id no request, busca a conta no banco, senão cria uma e salva
                if (model.Account_Id != null) {
                    Account? accountToAssign = _db.Accounts.Find(model.Account_Id);

                    if (accountToAssign == null) {
                        return new ResponseModel { Message = "Conta não encontrada" };
                    }

                    bool UserIsAlreadyAssigned = _db.ProfessorLists.Any(p => p.Account_Id == model.Account_Id);

                    if (UserIsAlreadyAssigned == true) {
                        return new ResponseModel { Message = "Usuário já está associado a um professor" };
                    }

                    // Associa a conta encontrada ao professor
                    professor.Account_Id = accountToAssign.Id;

                    // Atualizar dados do usuário, inserindo Role, mas se for admin, não deve alterar
                    // Os outros dados são iguais, então só repassá-los
                    UpdateAccountRequest updateAccountRequest = new() {
                        Id = accountToAssign.Id,
                        Name = accountToAssign.Name,
                        Email = accountToAssign.Email,
                        Phone = accountToAssign.Phone,
                        Role_Id = accountToAssign.Role_Id == ( int )Role.Admin ? ( int )Role.Admin : ( int )Role.Teacher
                    };

                    ResponseModel updateAccountResponse = _userService.Update(updateAccountRequest);

                    if (updateAccountResponse.Success == false) {
                        return updateAccountResponse;
                    }
                } else {
                    if (string.IsNullOrEmpty(model.Nome)) {
                        return new ResponseModel { Message = "Nome não deve ser vazio" };
                    }
                    if (string.IsNullOrEmpty(model.Telefone)) {
                        return new ResponseModel { Message = "Telefone não deve ser vazio" };
                    }
                    if (string.IsNullOrEmpty(model.Email)) {
                        return new ResponseModel { Message = "Email não deve ser vazio" };
                    }

                    CreateAccountRequest createAccountRequest = new() {
                        Name = model.Nome,
                        Phone = model.Telefone,
                        Email = model.Email,
                        Role_Id = ( int )Role.Teacher
                    };

                    ResponseModel createAccountResponse = _userService.Insert(createAccountRequest, ipAddress);

                    if (createAccountResponse.Success == false) {
                        return createAccountResponse;
                    }

                    // Associa o usuário recém criado ao professor
                    professor.Account_Id = createAccountResponse.Object!.Id;
                }

                if (professor == null) {
                    return new ResponseModel { Message = "Ocorreu algum erro ao criar o professor" };
                }

                _db.Professors.Add(professor);
                _db.SaveChanges();

                response.Message = "Professor cadastrado com sucesso";
                response.Object = _db.ProfessorLists.FirstOrDefault(p => p.Id == professor.Id);
                response.Success = true;
            } catch (Exception ex) {
                response.Message = "Falha ao cadastrar professor: " + ex.ToString();
            }

            return response;
        }

        public ResponseModel Update(UpdateProfessorRequest model)
        {
            ResponseModel response = new() { Success = false };

            try {
                Professor? professor = _db.Professors.Find(model.Id);

                if (professor == null) {
                    return new ResponseModel { Message = "Professor não encontrado" };
                }

                Account? account = _db.Accounts.Find(professor.Account_Id);

                if (account == null) {
                    return new ResponseModel { Message = "Conta não encontrada" };
                }

                response.OldObject = _db.ProfessorLists.SingleOrDefault(p => p.Id == professor.Id);

                // Só atribuir o nivel de certificacao passado na requisição se este existir, caso contrário, nulo
                bool NivelCertificacaoExists = _db.Professor_NivelCertificacaos.Any(n => n.Id == model.Professor_NivelCertificacao_Id);

                professor.Professor_NivelCertificacao_Id = NivelCertificacaoExists ? model.Professor_NivelCertificacao_Id : null;

                account.Name = model.Nome;
                account.Phone = model.Telefone;
                account.LastUpdated = TimeFunctions.HoraAtualBR();

                _db.Accounts.Update(account);
                _db.SaveChanges();

                professor.DataInicio = model.DataInicio;
                professor.CorLegenda = model.CorLegenda;
                professor.DataNascimento = model.DataNascimento;

                _db.Professors.Update(professor);
                _db.SaveChanges();

                response.Message = "Professor atualizado com sucesso";
                response.Object = _db.ProfessorLists.FirstOrDefault(p => p.Id == professor.Id);
                response.Success = true;
            } catch (Exception ex) {
                response.Message = "Ocorreu um erro ao atualizar professor" + ex.ToString();
            }

            return response;
        }

        public ResponseModel Delete(int professorId)
        {
            ResponseModel response = new() { Success = false };

            try {
                Professor? professor = _db.Professors.Find(professorId);

                if (professor == null) {
                    return new ResponseModel { Message = "Professor não encontrado" };
                }

                // Validations passed

                response.Object = _db.ProfessorLists.FirstOrDefault(p => p.Id == professorId);

                _db.Professors.Remove(professor);
                _db.SaveChanges();

                response.Message = "Turma excluída com sucesso";
                response.Success = true;
            } catch (Exception ex) {
                response.Message = "Falha ao deletar professor: " + ex.ToString();
            }

            return response;
        }

        public List<ApostilaList> GetAllApostilas()
        {
            List<ApostilaList> apostilas = _db.ApostilaLists.ToList();

            return apostilas;
        }

        public List<KitResponse> GetAllKits()
        {
            List<Apostila_Kit> listApostilaKits = _db.Apostila_Kits.ToList();

            List<KitResponse> listKitResponse = _mapper.Map<List<KitResponse>>(listApostilaKits);

            List<ApostilaList> apostilas = GetAllApostilas();

            foreach (KitResponse kit in listKitResponse) {
                kit.Apostilas = apostilas
                    .Where(a => a.Apostila_Kit_Id == kit.Id)
                    .ToList();
            }

            return listKitResponse;
        }

        public List<NivelCertificacaoModel> GetAllCertificacoes()
        {
            List<Professor_NivelCertificacao> certificacoes = _db.Professor_NivelCertificacaos.ToList();

            return _mapper.Map<List<NivelCertificacaoModel>>(certificacoes);
        }

        public bool HasTurmaTimeConflict(int professorId, int DiaSemana, TimeSpan Horario, int? IgnoredTurmaId = null)
        {
            try {
                Professor professor = _db.Professors.Find(professorId) ?? throw new Exception("IsProfessorAvailable : Professor não pode ser nulo");

                TimeSpan twoHourInterval = TimeSpan.FromHours(2);

                // Verifica se o professor é responsável por uma turma que está ocupando o mesmo dia e horário
                bool hasTurmaConflict = _db.Turmas
                .Where(t =>
                    t.Deactivated == null &&
                    t.Professor_Id == professor.Id &&
                    t.DiaSemana == DiaSemana &&
                    t.Id != IgnoredTurmaId // Se estou atualizando uma turma, devo ignorá-la na verificação de conflitos
                )
                .AsEnumerable() // Termina a query no banco, passando a responsabilidade do Any para o C#, queries do banco não lidam bem com TimeSpan
                .Any(t =>
                    Horario > t.Horario - twoHourInterval &&
                    Horario < t.Horario + twoHourInterval
                );

                return hasTurmaConflict;
            } catch (Exception ex) {
                throw new Exception($"Falha ao resgatar conflitos de turma do professor | {ex}");
            }
        }

        public bool HasEventoParticipacaoConflict(int professorId, DateTime Data, int DuracaoMinutos, int? IgnoredEventoId)
        {
            try {
                Professor professor = _db.Professors.Find(professorId) ?? throw new Exception("IsProfessorAvailable : Professor não pode ser nulo");

                // Definir intervalo do novo evento
                DateTime novoEventoInicio = Data;
                DateTime novoEventoFim = Data.AddMinutes(DuracaoMinutos);

                // Verifica se há conflito de participação
                bool hasParticipacaoConflict = _db.Evento_Participacao_Professors
                    .Where(e =>
                        e.Id != IgnoredEventoId &&
                        e.Evento.Deactivated == null &&
                        e.Professor_Id == professor.Id &&
                        e.Evento.Data > TimeFunctions.HoraAtualBR() &&
                        e.Evento.Data < novoEventoFim && // O evento existente começa antes do novo evento terminar
                        e.Evento.Data.AddMinutes(e.Evento.DuracaoMinutos) > novoEventoInicio // O evento existente termina depois do novo evento começar
                    ).Any();

                return hasParticipacaoConflict;
            } catch (Exception ex) {
                throw new Exception($"Falha ao resgatar conflitos de participação do professor | {ex}");
            }
        }
    }
}
