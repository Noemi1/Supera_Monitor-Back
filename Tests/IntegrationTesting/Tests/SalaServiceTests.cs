using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Sala;
using Supera_Monitor_Back.Services;
using Xunit;

namespace Tests.IntegrationTesting.Tests;

public class SalaServiceTests : BaseIntegrationTest {
    private readonly ISalaService sut;

    public SalaServiceTests(IntegrationTestWebAppFactory factory) : base(factory) {
        sut = new SalaService(_db, _mapper);
    }

    [Fact]
    public void Should_InsertSala() {
        // Arrange
        var sut = new SalaService(_db, _mapper);

        // Act
        ResponseModel response = sut.Insert(new CreateSalaRequest { Andar = 1, Descricao = "TesteCreate", NumeroSala = 101 });

        Sala? sala = _db.Salas.Find(response?.Object?.Id);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);

        Assert.NotNull(sala);
        Assert.Equal("TesteCreate", sala.Descricao);
    }

    [Fact]
    public void Should_UpdateSala() {
        // Arrange
        var sut = new SalaService(_db, _mapper);
        var salaResponse = sut.Insert(new CreateSalaRequest { Andar = 1, Descricao = "NotUpdated", NumeroSala = 102 });

        Assert.NotNull(salaResponse);
        Assert.True(salaResponse.Success);

        // Act
        var response = sut.Update(new UpdateSalaRequest { Id = salaResponse.Object!.Id, Andar = 2, Descricao = "Updated", NumeroSala = 102 });

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success, response.Message);

        Sala? sala = _db.Salas.Find(response.Object?.Id);

        Assert.NotNull(sala);
        Assert.Equal("Updated", sala.Descricao);
    }

    [Fact]
    public void Should_GetAllSalas() {
        // Arrange
        var sut = new SalaService(_db, _mapper);
        sut.Insert(new CreateSalaRequest { Andar = 1, Descricao = "Teste1", NumeroSala = 103 });
        sut.Insert(new CreateSalaRequest { Andar = 1, Descricao = "Teste2", NumeroSala = 104 });

        // Act
        var response = sut.GetAllSalas();

        var count = response.Where(s => s.NumeroSala == 103 || s.NumeroSala == 104).Count();

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public void ShouldNot_CreateSalaWithSameNumber() {
        // Arrange
        var sut = new SalaService(_db, _mapper);
        sut.Insert(new CreateSalaRequest { Andar = 5, Descricao = "Sala500", NumeroSala = 500 });

        // Act
        var response = sut.Insert(new CreateSalaRequest { Andar = 5, Descricao = "Sala500Again", NumeroSala = 500 });

        // Assert
        Assert.NotNull(response);
        Assert.False(response.Success, response.Message);
    }

    [Fact]
    public void ShouldNot_CreateSalaWithoutDescription() {
        // Arrange
        var sut = new SalaService(_db, _mapper);

        // Act
        var response = sut.Insert(new CreateSalaRequest { Andar = 5, Descricao = "", NumeroSala = 501 });

        // Assert
        Assert.NotNull(response);
        Assert.False(response.Success, response.Message);
    }
}
