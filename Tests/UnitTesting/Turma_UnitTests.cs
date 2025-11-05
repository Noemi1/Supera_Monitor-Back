using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Helpers;
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

    //[Theory]
    //[InlineData(12, 12)]
    //[InlineData(12, 6)]
    //[InlineData(0, 0)]
    //[InlineData(1, 1)]
    //public void ShouldNot_ExtractSequenciaVigenciaCorrectly(int capacidadeMaxima, int quantidadeAlunos) {
    //    Turma turma = new()
    //    {
    //        CapacidadeMaximaAlunos = capacidadeMaxima,
    //        Alunos = [],
    //    };

    //    DateTime now = TimeFunctions.HoraAtualBR();

    //    for (int i = 0; i < quantidadeAlunos; i++) {
    //        turma.Alunos.Add(new Aluno()
    //        {
    //            DataInicioVigencia = now,
    //            DataFimVigencia = now.AddMonths(6),
    //            Deactivated = null
    //        });
    //    }

    //    List<SequenciaVigencia> sequenciaVigencias = turma.ExtrairSequenciaVigencias();

    //    Assert.True(sequenciaVigencias.Count == quantidadeAlunos * 2);
    //}

    //[Fact]
    //public void ShouldNot_AllowIncompatibleVigencia() {
    //    var now = TimeFunctions.HoraAtualBR();

    //    Turma turma = new()
    //    {
    //        CapacidadeMaximaAlunos = 2,
    //        Alunos = [
    //            new() { Deactivated = null, DataInicioVigencia = now.Date, DataFimVigencia = now.AddMonths(6).Date },
    //            new() { Deactivated = null, DataInicioVigencia = now.Date, DataFimVigencia = now.AddMonths(6).Date },
    //        ],
    //    };

    //    Aluno novoAluno = new()
    //    {
    //        Deactivated = null,
    //        DataInicioVigencia = now.AddMonths(3).Date,
    //        DataFimVigencia = now.AddMonths(6).Date,
    //    };

    //    bool vigenciaValidada = turma.VerificarCompatibilidadeVigencia(novoAluno);

    //    Assert.False(vigenciaValidada);
    //}

    //[Fact]
    //public void Should_AllowCompatibleVigencia() {
    //    var now = TimeFunctions.HoraAtualBR();

    //    Turma turma = new()
    //    {
    //        CapacidadeMaximaAlunos = 2,
    //        Alunos = [
    //            new() { Deactivated = null, DataInicioVigencia = now.Date, DataFimVigencia = now.AddMonths(6).Date },
    //            new() { Deactivated = null, DataInicioVigencia = now.Date, DataFimVigencia = now.AddMonths(12).Date },
    //        ],
    //    };

    //    Aluno novoAluno = new()
    //    {
    //        Deactivated = null,
    //        DataInicioVigencia = now.AddMonths(6).Date,
    //        DataFimVigencia = now.AddMonths(12).Date,
    //    };

    //    bool vigenciaValida = turma.VerificarCompatibilidadeVigencia(novoAluno);

    //    Assert.True(vigenciaValida);
    //}
}
