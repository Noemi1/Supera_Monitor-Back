using AutoMapper;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Models.Accounts;

namespace Supera_Monitor_Back.Helpers {
    public class AutoMapperProfiles : Profile {

        public AutoMapperProfiles()
        {
            CreateMap<Account, AuthenticateResponse>();
        }
    }
}
