using Supera_Monitor_Back.Entities;
using Xunit;

namespace Tests.UnitTesting;

public class Turma_UnitTests {
    [Theory]
    [InlineData(5, 4, 1)]
    [InlineData(5, 3, 2)]
    [InlineData(5, 1, 0)]
    [InlineData(5, 0, 5)]
    [InlineData(5, 5, 0)]
    [InlineData(0, 0, 0)]
    public void Should_CheckTurmaAvailability(int vagasTotais, int vagasOcupadas, int vagasRequisitadas) {
        Turma turma = new()
        {
            CapacidadeMaximaAlunos = vagasTotais,
            Alunos = []
        };

        for (int i = 0; i < vagasOcupadas; i++) {
            turma.Alunos.Add(new() { Deactivated = null });
        }

        Assert.True(turma.PossuiVagas(vagasOcupadas: turma.Alunos.Count, vagasRequisitadas));
    }

    [Theory]
    [InlineData(5, 5, 1)]
    [InlineData(0, 0, 1)]
    [InlineData(5, 4, 2)]
    [InlineData(5, 0, 6)]
    public void ShouldNot_CheckTurmaAvailability(int vagasTotais, int vagasOcupadas, int vagasRequisitadas) {
        Turma turma = new()
        {
            CapacidadeMaximaAlunos = vagasTotais,
            Alunos = []
        };

        for (int i = 0; i < vagasOcupadas; i++) {
            turma.Alunos.Add(new() { Deactivated = null });
        }

        Assert.False(turma.PossuiVagas(vagasOcupadas: turma.Alunos.Count, vagasRequisitadas));
    }
}
