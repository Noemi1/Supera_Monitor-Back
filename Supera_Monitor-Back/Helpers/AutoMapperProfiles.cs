using AutoMapper;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.CRM;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Accounts;
using Supera_Monitor_Back.Models.Aluno;
using Supera_Monitor_Back.Models.Professor;
using Supera_Monitor_Back.Models.Turma;

namespace Supera_Monitor_Back.Helpers {
    public class AutoMapperProfiles : Profile {

        public AutoMapperProfiles()
        {
            CreateMap<_BaseModel, _BaseEntity>();
            CreateMap<_BaseEntity, _BaseModel>()
                .ForMember(dest => dest.Account_Created_Name, source => source.MapFrom(orig => (orig.Account_Created == null ? "" : $"{orig.Account_Created.Name} - {orig.Account_Created.Email}")));

            CreateMap<Log, LogModel>()
                .ForMember(dest => dest.AccountName, source => source.MapFrom(orig => (orig.Account == null ? "" : orig.Account.Name)))
                .ForMember(dest => dest.AccountEmail, source => source.MapFrom(orig => (orig.Account == null ? "" : orig.Account.Email)));
            CreateMap<AccountRole, AccountRoleModel>();
            CreateMap<TurmaTipo, TurmaTipoModel>();

            CreateMap<Account, AccountResponse>();
            CreateMap<Account, AuthenticateResponse>();
            CreateMap<CreateAccountRequest, Account>();

            CreateMap<CreateTurmaRequest, Turma>();
            CreateMap<UpdateTurmaRequest, Turma>();

            CreateMap<CreateProfessorRequest, Professor>();

            CreateMap<CreateAlunoRequest, Aluno>();
            CreateMap<UpdateAlunoRequest, Aluno>();
            CreateMap<UpdateAlunoRequest, Pessoa>();

            CreateMap<Professor_NivelAH, NivelModel>();
            CreateMap<Professor_NivelAbaco, NivelModel>();

        }
    }
}
