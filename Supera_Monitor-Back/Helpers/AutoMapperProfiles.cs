using AutoMapper;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Accounts;
using Supera_Monitor_Back.Models.Aluno;
using Supera_Monitor_Back.Models.Aula;
using Supera_Monitor_Back.Models.Checklist;
using Supera_Monitor_Back.Models.Pessoa;
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

            CreateMap<PerfilCognitivo, PerfilCognitivoModel>();
            CreateMap<PerfilCognitivoModel, PerfilCognitivo>();

            CreateMap<Account, AccountResponse>();
            CreateMap<Account, AuthenticateResponse>();
            CreateMap<CreateAccountRequest, Account>();

            CreateMap<CreateTurmaRequest, Turma>();
            CreateMap<UpdateTurmaRequest, Turma>();

            CreateMap<CreateProfessorRequest, Professor>();

            CreateMap<CreateAlunoRequest, Aluno>();
            CreateMap<UpdateAlunoRequest, Aluno>();
            CreateMap<UpdateAlunoRequest, UpdatePessoaRequest>();
            CreateMap<UpdatePessoaRequest, Pessoa>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Pessoa_Id));

            CreateMap<Professor_NivelCertificacao, NivelCertificacaoModel>();

            CreateMap<Pessoa_FaixaEtaria, PessoaFaixaEtariaModel>();
            CreateMap<Pessoa_Geracao, PessoaGeracaoModel>();
            CreateMap<Pessoa_Status, PessoaStatusModel>();
            CreateMap<Pessoa_Sexo, PessoaSexoModel>();

            CreateMap<AlunoList, CalendarioAlunoList>()
                .ForMember(dest => dest.Aluno_Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Aluno, opt => opt.MapFrom(src => src.Nome));
            CreateMap<CalendarioList, CalendarioResponse>();
            CreateMap<CalendarioList, Aula>();

            CreateMap<Apostila_Kit, KitResponse>();

            CreateMap<Checklist_Item, ChecklistItemModel>();
            CreateMap<Aluno_Checklist_Item, AlunoChecklistItemModel>();
        }
    }
}
