using AutoMapper;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models.Pessoa;

namespace Supera_Monitor_Back.Services {
    public interface IPessoaService {
        List<PessoaFaixaEtariaModel> GetAllFaixasEtarias();
        List<PessoaGeracaoModel> GetAllGeracoes();
        List<PessoaSexoModel> GetAllSexos();
        List<PessoaStatusModel> GetAllStatus();
    }

    public class PessoaService : IPessoaService {

        private readonly DataContext _db;
        private readonly IMapper _mapper;

        public PessoaService(DataContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public List<PessoaFaixaEtariaModel> GetAllFaixasEtarias()
        {
            List<Pessoa_FaixaEtaria> faixas = _db.Pessoa_FaixaEtaria.ToList();

            return _mapper.Map<List<PessoaFaixaEtariaModel>>(faixas);
        }

        public List<PessoaGeracaoModel> GetAllGeracoes()
        {
            List<Pessoa_Geracao> geracoes = _db.Pessoa_Geracoes.ToList();

            return _mapper.Map<List<PessoaGeracaoModel>>(geracoes);
        }

        public List<PessoaSexoModel> GetAllSexos()
        {
            List<Pessoa_Sexo> sexos = _db.Pessoa_Sexos.ToList();

            return _mapper.Map<List<PessoaSexoModel>>(sexos);
        }

        public List<PessoaStatusModel> GetAllStatus()
        {
            List<Pessoa_Status> status = _db.Pessoa_Statuses.ToList();

            return _mapper.Map<List<PessoaStatusModel>>(status);
        }
    }
}
