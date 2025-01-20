using AutoMapper;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Accounts;

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

            CreateMap<Account, AccountResponse>();
            CreateMap<Account, AuthenticateResponse>();
            CreateMap<CreateAccountRequest, Account>();
        }
    }
}
