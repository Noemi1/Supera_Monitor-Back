using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Pessoa;

namespace Supera_Monitor_Back.Services {
    public interface IPessoaService {
        List<PessoaFaixaEtariaModel> GetAllFaixasEtarias();
        List<PessoaGeracaoModel> GetAllGeracoes();
        List<PessoaSexoModel> GetAllSexos();
        List<PessoaStatusModel> GetAllStatus();

        ResponseModel Update(UpdatePessoaRequest model);
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
            List<Pessoa_Geracao> geracoes = _db.Pessoa_Geracao.ToList();

            return _mapper.Map<List<PessoaGeracaoModel>>(geracoes);
        }

        public List<PessoaSexoModel> GetAllSexos()
        {
            List<Pessoa_Sexo> sexos = _db.Pessoa_Sexo.ToList();

            return _mapper.Map<List<PessoaSexoModel>>(sexos);
        }

        public List<PessoaStatusModel> GetAllStatus()
        {
            List<Pessoa_Status> status = _db.Pessoa_Status.ToList();

            return _mapper.Map<List<PessoaStatusModel>>(status);
        }

        public ResponseModel Update(UpdatePessoaRequest model)
        {
            ResponseModel response = new() { Success = false };

            try {
                Pessoa? pessoa = _db.Pessoa.Find(model.Pessoa_Id);

                if (pessoa == null) {
                    return new ResponseModel { Message = "Pessoa não encontrada" };
                }

                // Pessoa não pode ter um nome vazio
                if (string.IsNullOrEmpty(model.Nome)) {
                    return new ResponseModel { Message = "Nome não pode ser nulo/vazio" };
                }

                // WARNING: Sending null values in the request will always override existing fields in Pessoa
                // If you'd like null values to be ignored do:
                // pessoa.Celular = model.Celular ?? pessoa.Celular;
                // However, this approach doesn't allow null, so you'd have to send an empty string
                // Else be careful with your requests
                _mapper.Map(model, pessoa);

                _db.Pessoa.Update(pessoa);
                _db.SaveChanges();

                response.Message = "Pessoa atualizada com sucesso";
                response.Object = _db.AlunoList.AsNoTracking().FirstOrDefault(a => a.Pessoa_Id == model.Pessoa_Id);
                response.Success = true;
            } catch (Exception ex) {
                response.Message = "Falha ao atualizar pessoa: " + ex.ToString();
            }

            return response;
        }
    }
}
