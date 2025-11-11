using Bogus;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Helpers;

namespace Tests.IntegrationTesting.Factories;
static class EventoFactory {
    public static Evento Create(DataContext db, Evento? evento) {
        var mockEventoGenerator = new Faker<Evento>()
            .RuleFor(e => e.Descricao, f => evento?.Descricao ?? f.Random.Words(2))
            .RuleFor(e => e.Data, f => evento?.Data ?? f.Date.Future())
            .RuleFor(e => e.Sala_Id, f => evento?.Sala_Id ?? 1) // Default Sala_Id -> Online
            .RuleFor(e => e.Evento_Tipo_Id, f => evento?.Evento_Tipo_Id ?? (int)EventoTipo.Aula)
            .RuleFor(e => e.Account_Created_Id, f => evento?.Account_Created_Id ?? 3)
            .RuleFor(e => e.CapacidadeMaximaAlunos, f => evento?.CapacidadeMaximaAlunos ?? 12)
            .RuleFor(e => e.DuracaoMinutos, f => evento?.DuracaoMinutos ?? 120)
            .RuleFor(e => e.Finalizado, f => evento?.Finalizado ?? false)
            .RuleFor(e => e.Created, TimeFunctions.HoraAtualBR())
            .RuleFor(e => e.Deactivated, f => evento?.Deactivated ?? null)
            .RuleFor(e => e.Evento_Aula, f => evento?.Evento_Aula ?? null);

        var mockEvento = mockEventoGenerator.Generate();

        db.Evento.Add(mockEvento);
        db.SaveChanges();

        return mockEvento;
    }

    public static Evento_Participacao_Aluno CreateParticipacaoAluno(DataContext db, Evento evento, Aluno aluno) {

        var participacaoAluno = new Evento_Participacao_Aluno
        {
            Evento_Id = evento.Id,
            Aluno_Id = aluno.Id,
            Apostila_Abaco_Id = aluno.Apostila_Abaco_Id,
            Apostila_AH_Id = aluno.Apostila_AH_Id,
            Presente = false,
            Observacao = null,
            Deactivated = null,
        };

        db.Evento_Participacao_Aluno.Add(participacaoAluno);
        db.SaveChanges();

        return participacaoAluno;
    }
}
