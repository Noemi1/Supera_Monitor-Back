using Microsoft.AspNetCore.Http;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models.Eventos.Aula;
using Supera_Monitor_Back.Services;
using Supera_Monitor_Back.Services.Email;
using Supera_Monitor_Back.Services.Eventos;
using Tests.IntegrationTesting.Factories;
using Xunit;

namespace Tests.IntegrationTesting.Tests;

public class AulaServiceTests : BaseIntegrationTest {
    private readonly ISalaService _salaService;
    private readonly IEmailService _emailService;
    private readonly IAccountService _accountService;
    private readonly IUserService _userService;
    private readonly IProfessorService _professorService;

    private readonly AulaService sut;

    public AulaServiceTests(IntegrationTestWebAppFactory factory) : base(factory) {
        _salaService = new SalaService(_db, _mapper);
        var httpContext = new DefaultHttpContext();
        httpContext.Items["Account"] = new Account { Id = 3 };
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };

        _emailService = new ConsoleEmailService();
        _accountService = new AccountService(_db, _appSettings, _mapper, _emailService, httpContextAccessor);
        _userService = new UserService(_db, _mapper, httpContextAccessor, _emailService, _accountService);
        _professorService = new ProfessorService(_db, _mapper, _userService);

        sut = new AulaService(_db, _mapper, _professorService, _salaService, httpContextAccessor);
    }

    [Fact]
    public void Should_CreateAulaForTurma() {
        // Arrange (constructor)
        var turma = TurmaFactory.Create(
            db: _db,
            turma: new Turma
            {
                Sala_Id = 1,
                Professor_Id = 1,
                CapacidadeMaximaAlunos = 12,
                DiaSemana = 1,
                Horario = new TimeSpan(10, 0, 0),
                Account_Created_Id = 3,
            },
            perfisCognitivos: [1, 2]);

        // Act
        var response = sut.InsertAulaForTurma(new CreateAulaTurmaRequest
        {
            Turma_Id = turma.Id,
            Sala_Id = (int)turma.Sala_Id!,
            Professor_Id = (int)turma.Professor_Id!,
            Data = TimeFunctions.HoraAtualBR(),
            Descricao = "Test Create Aula For Turma",
            DuracaoMinutos = 120,
            Observacao = "Test Obs",
            Roteiro_Id = null,
        });

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success, response.Message);

        var eventos = _db.CalendarioEventoLists.ToList();
        Assert.Single(eventos);

        var eventoAula = _db.Evento_Aulas.ToList();
        Assert.Single(eventoAula);
    }

    [Fact]
    public void Should_CreateAulaZero() {
        // Arrange (constructor)

        var alunoInAulaZero = AlunoFactory.Create(_db, new Aluno { });

        // Act
        var response = sut.InsertAulaZero(new CreateAulaZeroRequest
        {
            Alunos = [alunoInAulaZero.Id],
            Data = TimeFunctions.HoraAtualBR().AddDays(1),
            DuracaoMinutos = 60,
            Sala_Id = 1,
            Professor_Id = 1,
            Descricao = "Test Aula Zero",
        });

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success, response.Message);

        Aluno? aluno = _db.Alunos.Find(alunoInAulaZero.Id);
        Assert.NotNull(aluno);

        Assert.Equal(response.Object!.Id, aluno.AulaZero_Id);
    }

    //[Fact]
    //public void Should_MarkChecklistWhenAlunoIsInsertedInAulaZero() {
    //    // Arrange (constructor)

    //    var alunoInAulaZero = AlunoFactory.Create(_db, new Aluno { });

    //    // Act
    //    var response = sut.InsertAulaZero(new CreateAulaZeroRequest
    //    {
    //        Alunos = [alunoInAulaZero.Id],
    //        Data = TimeFunctions.HoraAtualBR().AddDays(1),
    //        DuracaoMinutos = 60,
    //        Sala_Id = 1,
    //        Professor_Id = 1,
    //        Descricao = "Test Aula Zero",
    //    });

    //    // Assert
    //    Assert.NotNull(response);
    //    Assert.True(response.Success, response.Message);

    //    Aluno? aluno = _db.Alunos.Find(alunoInAulaZero.Id);
    //    Assert.NotNull(aluno);

    //    Assert.Equal(response.Object!.Id, aluno.AulaZero_Id);
    //}
}
