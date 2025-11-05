using AutoMapper;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Accounts;
using Supera_Monitor_Back.Models.Aluno;
using Supera_Monitor_Back.Models.Apostila;
using Supera_Monitor_Back.Models.Checklist;
using Supera_Monitor_Back.Models.Eventos;
using Supera_Monitor_Back.Models.Eventos.Aula;
using Supera_Monitor_Back.Models.Monitoramento;
using Supera_Monitor_Back.Models.Pessoa;
using Supera_Monitor_Back.Models.Professor;
using Supera_Monitor_Back.Models.Restricao;
using Supera_Monitor_Back.Models.Roteiro;
using Supera_Monitor_Back.Models.Sala;
using Supera_Monitor_Back.Models.Turma;

namespace Supera_Monitor_Back.Helpers
{
    public class AutoMapperProfiles : Profile
    {

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
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => -1))
                .ForMember(dest => dest.Aluno_Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Aluno, opt => opt.MapFrom(src => src.Nome))
                .ForMember(dest => dest.Evento_Id, opt => opt.MapFrom(src => -1))
                .ForMember(dest => dest.Observacao, opt => opt.MapFrom(src => ""));

            CreateMap<Evento_Aula, AulaModel>()
                .ForMember(dest => dest.Professor, opt => opt.MapFrom(src => src.Professor.Account.Name))
                .ForMember(dest => dest.Roteiro, opt => opt.MapFrom(src => src.Roteiro.Tema))
                .ForMember(dest => dest.Turma, opt => opt.MapFrom(src => src.Turma.Nome));

            CreateMap<Apostila_Kit, KitResponse>();

            CreateMap<Checklist, ChecklistModel>();
            CreateMap<Checklist_Item, ChecklistItemModel>();
            CreateMap<Aluno_Checklist_Item, AlunoChecklistItemModel>();
            CreateMap<AlunoList, AlunoListWithChecklist>();
            CreateMap<Aluno_Historico, AlunoHistoricoModel>()
                .ForMember(dest => dest.Account_Created, opt => opt.MapFrom(src => src.Account.Name));

            CreateMap<CalendarioEventoList, CalendarioEventoList>();

            CreateMap<Sala, SalaModel>();

            CreateMap<Aluno_Restricao, RestricaoModel>()
                .ForMember(dest => dest.Aluno, opt => opt.MapFrom(src => src.Aluno.Pessoa.Nome));

            CreateMap<Evento_Aula, AulaModel>()
                .ForMember(dest => dest.Professor, opt => opt.MapFrom(src => src.Professor.Account.Name))
                .ForMember(dest => dest.Roteiro, opt => opt.MapFrom(src => src.Roteiro.Tema))
                .ForMember(dest => dest.Turma, opt => opt.MapFrom(src => src.Turma.Nome));

            CreateMap<Evento, EventoAulaModel>()
                .ForMember(dest => dest.Evento_Tipo, opt => opt.MapFrom(src => src.Evento_Tipo.Nome))
                .ForMember(dest => dest.Sala, opt => opt.MapFrom(src => $"{src.Sala.NumeroSala} - Andar: {src.Sala.Andar}"));

            CreateMap<Evento, EventoModel>()
                .ForMember(dest => dest.Evento_Tipo, opt => opt.MapFrom(src => src.Evento_Tipo.Nome))
                .ForMember(dest => dest.Sala, opt => opt.MapFrom(src => $"{src.Sala.NumeroSala} - Andar: {src.Sala.Andar}"));


            
            CreateMap<Roteiro, RoteiroModel>();
            CreateMap<RoteiroModel, Roteiro>();
            CreateMap<CreateRoteiroRequest, Roteiro>();
            CreateMap<UpdateRoteiroRequest, Roteiro>();

			// Monitoramento
            CreateMap<Roteiro, Monitoramento_Roteiro>();
            CreateMap<RoteiroModel, Monitoramento_Roteiro>();
            CreateMap<AlunoList, Monitoramento_Aluno>();
            CreateMap<CalendarioEventoList, Monitoramento_Aula>();
            CreateMap<CalendarioAlunoList, Monitoramento_Participacao>();



		}
    }
}
