using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Services;
using Xunit;

namespace Tests.UnitTesting;

public class Sala_UnitTests {
    [Theory]
    [InlineData("16:00:00")]
    [InlineData("17:00:00")]
    [InlineData("12:00:01")]
    [InlineData("17:59:59")]
    public void Should_IdentifyConflict(String HorarioStr) {
        TimeSpan Horario = TimeSpan.Parse(HorarioStr);

        Turma turma1 = new()
        {
            Horario = TimeSpan.FromHours(14),
            DiaSemana = (int)DayOfWeek.Monday,
        };

        Turma turma2 = new()
        {
            Horario = TimeSpan.FromHours(16),
            DiaSemana = (int)DayOfWeek.Monday,
        };

        List<Turma> turmas = [turma1, turma2];

        TimeSpan DuracaoPadrao = TimeSpan.FromHours(2);

        bool result = SalaService.FindRecurrentConflicts(Horario, DuracaoPadrao, turmas);

        Assert.True(result);
    }

    [Theory]
    [InlineData("11:00:00")]
    [InlineData("12:00:00")]
    [InlineData("18:00:00")]
    public void ShouldNot_IdentifyConflict(String HorarioStr) {
        TimeSpan Horario = TimeSpan.Parse(HorarioStr);

        Turma turma1 = new()
        {
            Horario = TimeSpan.FromHours(14),
            DiaSemana = (int)DayOfWeek.Monday,
        };

        Turma turma2 = new()
        {
            Horario = TimeSpan.FromHours(16),
            DiaSemana = (int)DayOfWeek.Monday,
        };

        List<Turma> turmas = [turma1, turma2];

        TimeSpan DuracaoPadrao = TimeSpan.FromHours(2);

        bool result = SalaService.FindRecurrentConflicts(Horario, DuracaoPadrao, turmas);

        Assert.False(result);
    }
}
