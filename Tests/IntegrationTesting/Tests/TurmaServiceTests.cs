using Microsoft.AspNetCore.Http;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Models.Turma;
using Supera_Monitor_Back.Services;
using Supera_Monitor_Back.Services.Email;
using Tests.IntegrationTesting.Factories;
using Xunit;

namespace Tests.IntegrationTesting.Tests;

public class TurmaServiceTests : BaseIntegrationTest {
    private readonly IEmailService _emailService;
    private readonly IAccountService _accountService;
    private readonly IUserService _userService;
    private readonly IProfessorService _professorService;
    private readonly ISalaService _salaService;

    private readonly TurmaService sut;

    public TurmaServiceTests(IntegrationTestWebAppFactory factory) : base(factory) {
        var httpContext = new DefaultHttpContext();
        httpContext.Items["Account"] = new Account { Id = 3 };
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };

        _emailService = new ConsoleEmailService();
        _accountService = new AccountService(_db, _appSettings, _mapper, _emailService, httpContextAccessor);
        _userService = new UserService(_db, _mapper, httpContextAccessor, _emailService, _accountService);
        _professorService = new ProfessorService(_db, _mapper, _userService);
        _salaService = new SalaService(_db, _mapper);

        sut = new TurmaService(_db, _mapper, httpContextAccessor, _professorService, _salaService);
    }

    [Fact]
    public void Should_CreateTurma() {
        // Arrange (constructor)

        // Act

        var response = sut.Insert(new CreateTurmaRequest
        {
            Nome = "TurmaTeste",
            DiaSemana = 1,
            Horario = new TimeSpan(10, 0, 0),
            CapacidadeMaximaAlunos = 12,
            Unidade_Id = 1,
            LinkGrupo = "TestContainers::LinkGrupo",
            Sala_Id = 1,
            Professor_Id = 1,
            PerfilCognitivo = [1, 2],
        });

        // Assert 

        Assert.NotNull(response.Object);
        Assert.True(response.Success, response.Message);

        Turma? turma = _db.Turmas.Find(response.Object!.Id);
        Assert.NotNull(turma);
    }

    [Fact]
    public void ShouldNot_CreateTurmaWithDeactivatedProfessor() {
        // Arrange (constructor)

        // Act
        var response = sut.Insert(new CreateTurmaRequest
        {
            Professor_Id = 3, // Professor desativado criado durante o seeding

            Nome = "TurmaTeste",
            DiaSemana = 1,
            Horario = new TimeSpan(12, 0, 0),
            CapacidadeMaximaAlunos = 12,
            Unidade_Id = 1,
            LinkGrupo = "TestContainers::LinkGrupo",
            Sala_Id = 1,
            PerfilCognitivo = [1, 2],
        });

        Assert.NotNull(response);
        Assert.False(response.Success, response.Message);
    }

    [Fact]
    public void ShouldNot_CreateTurmasWithProfessorTimeConflict() {
        // Arrange (constructor)

        sut.Insert(new CreateTurmaRequest
        {
            Professor_Id = 1,

            Nome = "TurmaTeste",
            DiaSemana = 1,
            Horario = new TimeSpan(14, 0, 0),
            CapacidadeMaximaAlunos = 12,
            Unidade_Id = 1,
            LinkGrupo = "TestContainers::LinkGrupo",
            Sala_Id = 1,
            PerfilCognitivo = [1, 2],
        });

        // Act
        var response = sut.Insert(new CreateTurmaRequest
        {
            Professor_Id = 1,

            Nome = "TurmaTeste",
            DiaSemana = 1,
            Horario = new TimeSpan(14, 59, 0),
            CapacidadeMaximaAlunos = 12,
            Unidade_Id = 1,
            LinkGrupo = "TestContainers::LinkGrupo",
            Sala_Id = 1,
            PerfilCognitivo = [1, 2],
        });

        // Assert
        Assert.NotNull(response);
        Assert.False(response.Success, response.Message);
    }

    [Fact]
    public void Should_DeactivateTurma() {
        // Arrange (constructor)
        var insertTurma = sut.Insert(new CreateTurmaRequest
        {
            CapacidadeMaximaAlunos = 2,

            Nome = "TurmaTeste",
            Sala_Id = 1,
            Professor_Id = 1,
            DiaSemana = 1,
            Horario = new TimeSpan(20, 0, 0),
            Unidade_Id = 1,
            LinkGrupo = "TestContainers::LinkGrupo",
            PerfilCognitivo = [1, 2],
        });

        var turmaId = insertTurma?.Object?.Id;

        Assert.NotNull(turmaId);

        // Act
        var response = sut.ToggleDeactivate(turmaId, "TestContainers");

        // Assert

        Assert.NotNull(response);
        Assert.True(response.Success, response.Message);

        Turma? turma = _db.Turmas.Find(turmaId);

        Assert.NotNull(turma);
        Assert.NotNull(turma.Deactivated);
    }

    [Fact]
    public void ShouldNot_CreateTurmasWithSalaConflict() {
        // Arrange (constructor)

        sut.Insert(new CreateTurmaRequest
        {
            Sala_Id = 2,
            Professor_Id = 1,

            Nome = "TurmaTeste",
            DiaSemana = 1,
            Horario = new TimeSpan(14, 0, 0),
            CapacidadeMaximaAlunos = 12,
            Unidade_Id = 1,
            LinkGrupo = "TestContainers::LinkGrupo",
            PerfilCognitivo = [1, 2],
        });

        // Act
        var response = sut.Insert(new CreateTurmaRequest
        {
            Sala_Id = 2,
            Professor_Id = 2,

            Nome = "TurmaConflictTest",
            DiaSemana = 1,
            Horario = new TimeSpan(15, 59, 0),
            CapacidadeMaximaAlunos = 12,
            Unidade_Id = 1,
            LinkGrupo = "TestContainers::LinkGrupo",
            PerfilCognitivo = [1, 2],
        });

        // Assert
        Assert.NotNull(response);
        Assert.False(response.Success, response.Message);
    }

    [Fact]
    public void ShouldNot_DecreaseCapacidadeMaximaBelowAlunosInTurmaAmount() {
        // Arrange (constructor)
        var insertTurma = sut.Insert(new CreateTurmaRequest
        {
            CapacidadeMaximaAlunos = 2,

            Nome = "TurmaTeste",
            Sala_Id = 1,
            Professor_Id = 1,
            DiaSemana = 1,
            Horario = new TimeSpan(20, 0, 0),
            Unidade_Id = 1,
            LinkGrupo = "TestContainers::LinkGrupo",
            PerfilCognitivo = [1, 2],
        });

        var turmaId = insertTurma?.Object?.Id;

        Assert.NotNull(turmaId);

        AlunoFactory.Create(_db, new Aluno { Turma_Id = turmaId });
        AlunoFactory.Create(_db, new Aluno { Turma_Id = turmaId });

        // Act

        var response = sut.Update(new UpdateTurmaRequest
        {
            Id = turmaId,
            CapacidadeMaximaAlunos = 1, // Tentando diminuir abaixo do número de alunos matriculados,

            Nome = "TurmaTeste",
            Sala_Id = 1,
            Professor_Id = 1,
            DiaSemana = 1,
            Horario = new TimeSpan(20, 0, 0),
            Unidade_Id = 1,
            LinkGrupo = "TestContainers:LinkGrupo",
            PerfilCognitivo = [1, 2],
        });

        // Assert

        Assert.NotNull(response);
        Assert.False(response.Success, response.Message);
    }

    [Fact]
    public void ShouldNot_RemovePerfilCognitivoIfAnyAlunoInTurmaHasIt() {
        // Arrange (constructor)
        var insertTurma = sut.Insert(new CreateTurmaRequest
        {
            CapacidadeMaximaAlunos = 2,

            Nome = "TurmaTeste",
            Sala_Id = 1,
            Professor_Id = 1,
            DiaSemana = 1,
            Horario = new TimeSpan(20, 0, 0),
            Unidade_Id = 1,
            LinkGrupo = "TestContainers::LinkGrupo",
            PerfilCognitivo = [1, 2],
        });

        var turmaId = insertTurma?.Object?.Id;

        Assert.NotNull(turmaId);

        AlunoFactory.Create(_db, new Aluno { Turma_Id = turmaId, PerfilCognitivo_Id = 2 });

        // Act

        var response = sut.Update(new UpdateTurmaRequest
        {
            Id = turmaId,
            CapacidadeMaximaAlunos = 2,

            Nome = "TurmaTeste",
            Sala_Id = 1,
            Professor_Id = 1,
            DiaSemana = 1,
            Horario = new TimeSpan(20, 0, 0),
            Unidade_Id = 1,
            LinkGrupo = "TestContainers:LinkGrupo",
            PerfilCognitivo = [1],
        });

        // Assert

        Assert.NotNull(response);
        Assert.False(response.Success, response.Message);
        Assert.Equal("Não é possível alterar um dos perfis cognitivos da turma, um dos alunos matriculados possui esse perfil.", response.Message);
    }

    [Fact]
    public void Should_AlterSalaIfValidateRestricaoMobilidadeIsValid() {
        // Arrange (constructor)

        var insertTurma = sut.Insert(new CreateTurmaRequest
        {
            Nome = "TurmaRestricaoMobilidade",
            Sala_Id = 1, // Sala online

            CapacidadeMaximaAlunos = 5,
            Professor_Id = 1,
            DiaSemana = 1,
            Horario = new TimeSpan(20, 0, 0),
            Unidade_Id = 1,
            LinkGrupo = "TestContainers::LinkGrupo",
            PerfilCognitivo = [1, 2],
        });

        var turmaId = insertTurma?.Object?.Id;

        Assert.NotNull(turmaId);

        AlunoFactory.Create(_db, new Aluno { Turma_Id = turmaId, PerfilCognitivo_Id = 1, RestricaoMobilidade = true });
        AlunoFactory.Create(_db, new Aluno { Turma_Id = turmaId, PerfilCognitivo_Id = 2, RestricaoMobilidade = false });

        // Act

        var response = sut.Update(new UpdateTurmaRequest
        {
            Id = turmaId,
            CapacidadeMaximaAlunos = 5,

            Nome = "TurmaRestricaoMobilidade",
            Sala_Id = 2,
            Professor_Id = 1,
            DiaSemana = 1,
            Horario = new TimeSpan(20, 0, 0),
            Unidade_Id = 1,
            LinkGrupo = "TestContainers:LinkGrupo",
            PerfilCognitivo = [1, 2],
        });

        Assert.NotNull(response);
        Assert.True(response.Success, response.Message);
    }

    [Fact]
    public void ShouldNot_AlterSalaIfValidateRestricaoMobilidadeIsInvalid() {
        // Arrange (constructor)

        var insertTurma = sut.Insert(new CreateTurmaRequest
        {
            Nome = "TurmaRestricaoMobilidade",
            Sala_Id = 1, // Sala online

            CapacidadeMaximaAlunos = 5,
            Professor_Id = 1,
            DiaSemana = 1,
            Horario = new TimeSpan(20, 0, 0),
            Unidade_Id = 1,
            LinkGrupo = "TestContainers::LinkGrupo",
            PerfilCognitivo = [1, 2],
        });

        var turmaId = insertTurma?.Object?.Id;

        Assert.NotNull(turmaId);

        AlunoFactory.Create(_db, new Aluno { Turma_Id = turmaId, PerfilCognitivo_Id = 1, RestricaoMobilidade = true });
        AlunoFactory.Create(_db, new Aluno { Turma_Id = turmaId, PerfilCognitivo_Id = 2, RestricaoMobilidade = false });

        // Act

        var response = sut.Update(new UpdateTurmaRequest
        {
            Id = turmaId,
            CapacidadeMaximaAlunos = 5,

            Nome = "TurmaRestricaoMobilidade",
            Sala_Id = 4,
            Professor_Id = 1,
            DiaSemana = 1,
            Horario = new TimeSpan(20, 0, 0),
            Unidade_Id = 1,
            LinkGrupo = "TestContainers:LinkGrupo",
            PerfilCognitivo = [1, 2],
        });

        Assert.NotNull(response);
        Assert.False(response.Success, response.Message);
        Assert.Equal("A sala destino selecionada não possui acessibilidade para os alunos com restrições de mobilidade já cadastrados.", response.Message);
    }
}
