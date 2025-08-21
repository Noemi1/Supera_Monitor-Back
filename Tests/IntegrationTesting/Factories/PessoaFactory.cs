using Bogus;
using Bogus.Extensions.Brazil;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Helpers;

namespace Tests.IntegrationTesting.Factories;

static class PessoaFactory {
    public static Pessoa Create(DataContext db, Pessoa? pessoa) {
        var mockPessoaGenerator = new Faker<Pessoa>()
            .RuleFor(p => p.Nome, f => pessoa?.Nome ?? f.Person.FullName)
            .RuleFor(p => p.Email, f => pessoa?.Email ?? f.Internet.Email())
            .RuleFor(p => p.Telefone, f => pessoa?.Telefone ?? f.Phone.PhoneNumber())
            .RuleFor(p => p.Celular, f => pessoa?.Celular ?? f.Phone.PhoneNumber())
            .RuleFor(p => p.CPF, f => pessoa?.CPF ?? f.Person.Cpf())
            .RuleFor(p => p.Endereco, f => pessoa?.Endereco ?? f.Address.FullAddress())
            .RuleFor(p => p.DataNascimento, f => f.Person.DateOfBirth)
            .RuleFor(p => p.DataCadastro, TimeFunctions.HoraAtualBR())
            .RuleFor(p => p.DataEntrada, TimeFunctions.HoraAtualBR());

        var mockPessoa = mockPessoaGenerator.Generate();

        db.Pessoas.Add(mockPessoa);
        db.SaveChanges();

        return mockPessoa;
    }
}
