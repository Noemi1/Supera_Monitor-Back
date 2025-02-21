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
            ProfessorList? professor = _db.ProfessorList.FirstOrDefault(p => p.Id == professorId);

            if (professor == null) {
                throw new Exception("Professor não encontrado.");
            }

            return professor;
        }

        public List<ProfessorList> GetAll()
        {
            List<ProfessorList> professores = _db.ProfessorList.OrderBy(p => p.Nome).ToList();

            return professores;
        }

        public ResponseModel Insert(CreateProfessorRequest model, string ipAddress)
        {
            ResponseModel response = new() { Success = false };

            try {
                Professor professor = new() {
                    DataInicio = model.DataInicio,
                    CorLegenda = model.CorLegenda,
                };

                // Só atribuir o nivel de certificacao passado na requisição se este existir, caso contrário, nulo
                bool NivelCertificacaoExists = _db.Professor_NivelCertificacao.Any(n => n.Id == model.Professor_NivelCertificacao_Id);

                professor.Professor_NivelCertificacao_Id = NivelCertificacaoExists ? model.Professor_NivelCertificacao_Id : null;

                // Se for passado um Account_Id no request, busca a conta no banco, senão cria uma e salva
                if (model.Account_Id != null) {
                    Account? accountToAssign = _db.Account.Find(model.Account_Id);

                    if (accountToAssign == null) {
                        return new ResponseModel { Message = "Conta não encontrada" };
                    }

                    bool UserIsAlreadyAssigned = _db.ProfessorList.Any(p => p.Account_Id == model.Account_Id);

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

                _db.Professor.Add(professor);
                _db.SaveChanges();

                response.Message = "Professor cadastrado com sucesso";
                response.Object = _db.ProfessorList.FirstOrDefault(p => p.Id == professor.Id);
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
                Professor? professor = _db.Professor.Find(model.Id);

                if (professor == null) {
                    return new ResponseModel { Message = "Professor não encontrado" };
                }

                Account? account = _db.Account.Find(professor.Account_Id);

                if (account == null) {
                    return new ResponseModel { Message = "Conta não encontrada" };
                }

                response.OldObject = _db.ProfessorList.SingleOrDefault(p => p.Id == professor.Id);

                // Só atribuir o nivel de certificacao passado na requisição se este existir, caso contrário, nulo
                bool NivelCertificacaoExists = _db.Professor_NivelCertificacao.Any(n => n.Id == model.Professor_NivelCertificacao_Id);

                professor.Professor_NivelCertificacao_Id = NivelCertificacaoExists ? model.Professor_NivelCertificacao_Id : null;

                account.Name = model.Nome;
                account.Phone = model.Telefone;
                account.LastUpdated = TimeFunctions.HoraAtualBR();

                _db.Account.Update(account);

                professor.DataInicio = model.DataInicio;
                professor.CorLegenda = model.CorLegenda;

                _db.Professor.Update(professor);

                _db.SaveChanges();

                response.Message = "Professor atualizado com sucesso";
                response.Object = _db.ProfessorList.FirstOrDefault(p => p.Id == professor.Id);
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
                Professor? professor = _db.Professor.Find(professorId);

                if (professor == null) {
                    return new ResponseModel { Message = "Professor não encontrado" };
                }

                // Validations passed

                response.Object = _db.ProfessorList.FirstOrDefault(p => p.Id == professorId);

                _db.Professor.Remove(professor);
                _db.SaveChanges();

                response.Message = "Turma excluída com sucesso";
                response.Success = true;
            } catch (Exception ex) {
                response.Message = "Falha ao deletar professor: " + ex.ToString();
            }

            return response;
        }

        //public List<NivelModel> GetAllNiveisAh()
        //{
        //    List<Professor_NivelAH> niveis = _db.Professor_NivelAH.ToList();

        //    return _mapper.Map<List<NivelModel>>(niveis);
        //}

        //public List<NivelModel> GetAllNiveisAbaco()
        //{
        //    List<Professor_NivelAbaco> niveis = _db.Professor_NivelAbaco.ToList();

        //    return _mapper.Map<List<NivelModel>>(niveis);
        //}


        public List<ApostilaList> GetAllApostilas()
        {
            List<ApostilaList> apostilas = _db.ApostilaList.ToList();

            return apostilas;
        }

        public List<KitResponse> GetAllKits()
        {
            List<Apostila_Kit> listApostilaKits = _db.Apostila_Kit.ToList();

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
            List<Professor_NivelCertificacao> certificacoes = _db.Professor_NivelCertificacao.ToList();

            return _mapper.Map<List<NivelCertificacaoModel>>(certificacoes);
        }
    }
}
