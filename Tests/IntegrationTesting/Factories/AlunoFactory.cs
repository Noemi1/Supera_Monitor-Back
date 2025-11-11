using Bogus;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Helpers;

namespace Tests.IntegrationTesting.Factories;
static class AlunoFactory {
    public static Aluno Create(DataContext db, Aluno? aluno) {
        var pessoa = PessoaFactory.Create(db, null); // P/ existir um aluno, sempre precisa existir uma pessoa

        var mockAlunoGenerator = new Faker<Aluno>()
            .RuleFor(a => a.Pessoa_Id, pessoa.Id)

            .RuleFor(a => a.Turma_Id, f => aluno?.Turma_Id ?? null)
            .RuleFor(a => a.PerfilCognitivo_Id, f => aluno?.PerfilCognitivo_Id ?? null)
            .RuleFor(a => a.RestricaoMobilidade, f => aluno?.RestricaoMobilidade ?? null)

            .RuleFor(a => a.Apostila_Kit_Id, f => aluno?.Apostila_Kit_Id ?? null)
            .RuleFor(a => a.Apostila_Abaco_Id, f => aluno?.Apostila_Abaco_Id ?? null)
            .RuleFor(a => a.NumeroPaginaAbaco, f => aluno?.NumeroPaginaAbaco ?? null)
            .RuleFor(a => a.Apostila_AH_Id, f => aluno?.Apostila_AH_Id ?? null)
            .RuleFor(a => a.NumeroPaginaAH, f => aluno?.NumeroPaginaAH ?? null)

            .RuleFor(a => a.RM, f => Supera_Monitor_Back.Helpers.Utils.GenerateRM(db))
            .RuleFor(a => a.SenhaApp, f => aluno?.SenhaApp ?? "Supera@123")

            .RuleFor(a => a.AulaZero_Id, f => aluno?.AulaZero_Id ?? null)
            .RuleFor(a => a.PrimeiraAula_Id, f => aluno?.PrimeiraAula_Id ?? null)

            .RuleFor(a => a.Created, TimeFunctions.HoraAtualBR())
            .RuleFor(a => a.AspNetUsers_Created_Id, f => aluno?.AspNetUsers_Created_Id ?? "testcontainers");

        var mockAluno = mockAlunoGenerator.Generate();

        db.Aluno.Add(mockAluno);
        db.SaveChanges();

        return mockAluno;
    }
}
