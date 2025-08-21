using Bogus;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Helpers;

namespace Tests.IntegrationTesting.Factories;

static class TurmaFactory {
    public static Turma Create(DataContext db, Turma? turma, List<int>? perfisCognitivos) {
        var mockTurmaGenerator = new Faker<Turma>()
            .RuleFor(t => t.Professor_Id, turma?.Professor_Id ?? null)
            .RuleFor(t => t.Sala_Id, turma?.Sala_Id ?? null)
            .RuleFor(t => t.Unidade_Id, f => turma?.Unidade_Id ?? 1)

            .RuleFor(t => t.Nome, f => turma?.Nome ?? $"Turma ${f.Internet.UserName}")
            .RuleFor(t => t.Horario, f => turma?.Horario ?? null)
            .RuleFor(t => t.DiaSemana, f => turma?.DiaSemana ?? null)
            .RuleFor(t => t.CapacidadeMaximaAlunos, f => turma?.CapacidadeMaximaAlunos ?? 12)
            .RuleFor(t => t.LinkGrupo, f => turma?.LinkGrupo ?? "TestContainer::LinkGrupo")

            .RuleFor(t => t.Created, TimeFunctions.HoraAtualBR())
            .RuleFor(t => t.Deactivated, f => turma?.Deactivated ?? null)
            .RuleFor(t => t.Account_Created_Id, f => turma?.Account_Created_Id ?? 3);

        var mockTurma = mockTurmaGenerator.Generate();

        db.Turmas.Add(mockTurma);
        db.SaveChanges();

        if (perfisCognitivos is not null && perfisCognitivos.Count > 0) {
            foreach (var perfil in perfisCognitivos) {
                db.Turma_PerfilCognitivo_Rels.Add(new Turma_PerfilCognitivo_Rel
                {
                    PerfilCognitivo_Id = perfil,
                    Turma_Id = mockTurma.Id,
                });
            }

            db.SaveChanges();
        }

        return mockTurma;
    }
}
