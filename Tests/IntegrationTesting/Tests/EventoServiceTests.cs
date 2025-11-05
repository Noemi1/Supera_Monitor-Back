using Microsoft.AspNetCore.Http;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models.Eventos;
using Supera_Monitor_Back.Models.Eventos.Participacao;
using Supera_Monitor_Back.Services;
using Supera_Monitor_Back.Services.Email;
using Supera_Monitor_Back.Services.Eventos;
using Tests.IntegrationTesting.Factories;
using Xunit;

namespace Tests.IntegrationTesting.Tests;

public class EventoServiceTests : BaseIntegrationTest {
    private readonly ISalaService _salaService;
    private readonly IEmailService _emailService;
    private readonly IAccountService _accountService;
    private readonly IUserService _userService;
    private readonly IProfessorService _professorService;
    private readonly IRoteiroService _roteiroService;

    private readonly EventoService sut;

    public EventoServiceTests(IntegrationTestWebAppFactory factory) : base(factory) {
        _salaService = new SalaService(_db, _mapper);
        var httpContext = new DefaultHttpContext();
        httpContext.Items["Account"] = new Account { Id = 3 };
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };

        _emailService = new ConsoleEmailService();
        
		_accountService = new AccountService(_db, _appSettings, _mapper, _emailService, httpContextAccessor);
        
		_userService = new UserService(_db, _mapper, httpContextAccessor, _emailService, _accountService);
        
		_professorService = new ProfessorService(_db, _mapper, _userService);
		
		_roteiroService = new RoteiroService(_db, _mapper, httpContextAccessor);

		sut = new EventoService(_db, _mapper, _professorService, _salaService, _roteiroService, httpContextAccessor);
    }

    [Fact]
    public void Deve_FinalizarEvento() {
        // Arrange (constructor)

        var turma = TurmaFactory.Create(_db, new Turma
        {
            DiaSemana = 1,
            Horario = new TimeSpan(10, 0, 0),
            Sala_Id = 1,
            Professor_Id = 1,
            Nome = "Test Turma",
            CapacidadeMaximaAlunos = 12,
            Unidade_Id = 1,
            Account_Created_Id = 3,
        }, null);

        Assert.NotNull(turma);

        var evento = EventoFactory.Create(_db, new Evento
        {
            Descricao = "Test Evento Finalizar",
            Data = TimeFunctions.HoraAtualBR().AddDays(1),
            Evento_Tipo_Id = (int)EventoTipo.Aula,
            DuracaoMinutos = 120,
            Sala_Id = 1,
            CapacidadeMaximaAlunos = 10,
            Account_Created_Id = 3, // Admin
            Created = TimeFunctions.HoraAtualBR(),
            Evento_Aula = new Evento_Aula
            {
                Professor_Id = (int)turma.Professor_Id!,
                Roteiro_Id = null,
                Turma_Id = turma.Id,
            },
        });

        Assert.NotNull(evento);

        // Act
        var response = sut.Finalizar(new FinalizarEventoRequest
        {
            Evento_Id = evento.Id,
            Observacao = "Evento concluído",
            Alunos = [],
            Professores = [],
        });

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success, response.Message);

        var eventoResult = _db.Eventos.Find(evento.Id);
        Assert.NotNull(eventoResult);
        Assert.True(eventoResult.Finalizado);

        var eventoAulaResult = _db.Evento_Aulas.Find(evento.Id);
        Assert.NotNull(eventoAulaResult);
        Assert.Equal(turma.Id, eventoAulaResult.Turma_Id);
    }

    [Fact]
    public void Deve_FinalizarAulaZero() {
        // Arrange (constructor)

        int perfilCognitivoId = 1;

        var turma = TurmaFactory.Create(_db, new Turma
        {
            DiaSemana = 1,
            Horario = new TimeSpan(10, 0, 0),
            Sala_Id = 1,
            Professor_Id = 1,
            Nome = "Test Turma",
            CapacidadeMaximaAlunos = 12,
            Unidade_Id = 1,
            Account_Created_Id = 3,
        }, [perfilCognitivoId]);

        var evento = EventoFactory.Create(_db, new Evento
        {
            Descricao = "Test Evento Finalizar",
            Data = TimeFunctions.HoraAtualBR().AddDays(1),
            Evento_Tipo_Id = (int)EventoTipo.AulaZero,
            DuracaoMinutos = 60,
            Sala_Id = 1,
            CapacidadeMaximaAlunos = 999,
            Account_Created_Id = 3, // Admin
            Created = TimeFunctions.HoraAtualBR(),
            Evento_Aula = new Evento_Aula
            {
                Professor_Id = 1,
                Roteiro_Id = null,
                Turma_Id = null,
            },
        });

        Assert.NotNull(evento);

        var aluno = AlunoFactory.Create(_db, new Aluno { });

        var participacao = EventoFactory.CreateParticipacaoAluno(_db, evento, aluno);

        // Act
        var response = sut.FinalizarAulaZero(new FinalizarAulaZeroRequest
        {
            Evento_Id = evento.Id,
            Observacao = "Aula zero finalizada",
            Alunos = [
                new() {
                    Aluno_Id = aluno.Id,
                    Participacao_Id = participacao.Id,
                    Apostila_Kit_Id = 1,
                    PerfilCognitivo_Id = perfilCognitivoId,
                    Presente = true,
                    Turma_Id = turma.Id,
                },
            ],
        });

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success, response.Message);

        var eventoResult = _db.Eventos.Find(evento.Id);
        Assert.NotNull(eventoResult);
        Assert.True(eventoResult.Finalizado);

        var alunoResult = _db.Alunos.Find(aluno.Id);
        Assert.NotNull(alunoResult);

        Assert.Equal(turma.Id, alunoResult.Turma_Id);
        Assert.Equal(perfilCognitivoId, alunoResult.PerfilCognitivo_Id);
        Assert.Equal(1, alunoResult.Apostila_Kit_Id);
    }

    [Fact]
    public void Deve_MarcarPresencaDoAluno() {
        // Arrange (constructor)

        var turma = TurmaFactory.Create(_db, new Turma
        {
            DiaSemana = 1,
            Horario = new TimeSpan(10, 0, 0),
            Sala_Id = 1,
            Professor_Id = 1,
            Nome = "Test Turma",
            CapacidadeMaximaAlunos = 12,
            Unidade_Id = 1,
            Account_Created_Id = 3,
        }, [1, 2]);

        Assert.NotNull(turma);

        var alunoPresente = AlunoFactory.Create(_db, new Aluno { Turma_Id = turma.Id, PerfilCognitivo_Id = 1, Apostila_Kit_Id = 1, Apostila_Abaco_Id = 1, Apostila_AH_Id = 3 });
        var alunoFaltante = AlunoFactory.Create(_db, new Aluno { Turma_Id = turma.Id, PerfilCognitivo_Id = 2, Apostila_Kit_Id = 1, Apostila_Abaco_Id = 1, Apostila_AH_Id = 3 });

        var evento = EventoFactory.Create(_db, new Evento
        {
            Descricao = "Test Evento Finalizar",
            Data = TimeFunctions.HoraAtualBR().AddDays(1),
            Evento_Tipo_Id = (int)EventoTipo.Aula,
            DuracaoMinutos = 120,
            Sala_Id = 1,
            CapacidadeMaximaAlunos = 10,
            Account_Created_Id = 3, // Admin
            Created = TimeFunctions.HoraAtualBR(),
            Evento_Aula = new Evento_Aula
            {
                Professor_Id = (int)turma.Professor_Id!,
                Roteiro_Id = null,
                Turma_Id = turma.Id,
            },
        });

        Assert.NotNull(evento);

        var participacaoPresente = EventoFactory.CreateParticipacaoAluno(_db, evento, alunoPresente);
        var participacaoFaltante = EventoFactory.CreateParticipacaoAluno(_db, evento, alunoFaltante);

        // Act
        var response = sut.Finalizar(new FinalizarEventoRequest
        {
            Evento_Id = evento.Id,
            Observacao = "Evento concluído",
            Alunos = [
                new ParticipacaoAlunoModel { Participacao_Id = participacaoPresente.Id, Presente = true, Apostila_Abaco_Id = 1, Apostila_Ah_Id = 3 },
                new ParticipacaoAlunoModel { Participacao_Id = participacaoFaltante.Id, Presente = false, Apostila_Abaco_Id = 1, Apostila_Ah_Id = 3 }
            ],
        });

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success, response.Message);

        var presenteResult = _db.Evento_Participacao_Alunos.Find(participacaoPresente.Id);
        Assert.NotNull(presenteResult);
        Assert.True(presenteResult.Presente);

        var faltanteResult = _db.Evento_Participacao_Alunos.Find(participacaoFaltante.Id);
        Assert.NotNull(faltanteResult);
        Assert.False(faltanteResult.Presente);
    }

    //[Fact]
    //public void Deve_ObterCalendario() {
    //    // Arrange (constructor)
    //    var startOfWeek = DateTime.Now.Date.AddDays(-(int)DateTime.Now.DayOfWeek);
    //    var endOfWeek = startOfWeek.AddDays(6);

    //    var turma = TurmaFactory.Create(_db, new Turma
    //    {
    //        DiaSemana = 5,
    //        Horario = new TimeSpan(18, 0, 0),
    //        Sala_Id = 1,
    //        Professor_Id = 1,
    //        Nome = "Test Turma",
    //        CapacidadeMaximaAlunos = 12,
    //        Unidade_Id = 1,
    //        Account_Created_Id = 3,
    //    }, [1, 2]);

    //    AlunoFactory.Create(_db, new Aluno { Turma_Id = turma.Id, PerfilCognitivo_Id = 1, Apostila_Kit_Id = 1, Apostila_Abaco_Id = 1, Apostila_AH_Id = 3 });

    //    // Act
    //    var response = sut.GetCalendario(new CalendarioRequest { IntervaloDe = startOfWeek, IntervaloAte = endOfWeek });

    //    // Assert
    //    var countEventos = response.Count;

    //    // Padrão: 1 Evento de oficina, 3 Eventos de reunião (1 geral, 1 monitoramento, 1 pedagógica)
    //    // Recorrentes: 1 Aula semanal para cada turma ativa
    //    // Eventos isolados: Aula zero, Superação, Aula extra

    //    Assert.NotNull(response);
    //    Assert.Equal(5, countEventos);

    //    var eventoTurma = response.SingleOrDefault(e => e.Evento_Tipo_Id == (int)EventoTipo.Aula);
    //    Assert.NotNull(eventoTurma);
    //    Assert.Single(eventoTurma.Alunos);
    //}

    [Fact]
    public void Deve_InserirParticipacao() {
        var evento = EventoFactory.Create(_db, new Evento
        {
            Data = TimeFunctions.HoraAtualBR(),
            Sala_Id = 1,
            CapacidadeMaximaAlunos = 12,
            DuracaoMinutos = 60,
            Evento_Tipo_Id = (int)EventoTipo.AulaExtra,
            Evento_Aula = new Evento_Aula
            {
                Roteiro_Id = null,
                Professor_Id = 1,
                Turma_Id = null,
            },
        });

        var aluno = AlunoFactory.Create(_db, new Aluno { PerfilCognitivo_Id = 1, Apostila_Kit_Id = 1, Apostila_Abaco_Id = 1, Apostila_AH_Id = 3 });

        var response = sut.InsertParticipacao(new() { Aluno_Id = aluno.Id, Evento_Id = evento.Id });

        Assert.NotNull(response);
        Assert.True(response.Success);

        var participacao = _db.Evento_Participacao_Alunos.FirstOrDefault(p => p.Evento_Id == evento.Id);

        Assert.NotNull(participacao);
        Assert.Equal(evento.Id, participacao.Evento_Id);
    }

    [Fact]
    public void Deve_AtualizarDadosAlunoAoFinalizarAulaZero() {
        // Arrange (constructor)

        var turma = TurmaFactory.Create(_db, new Turma
        {
            Nome = "Test Turma",
            DiaSemana = 1,
            Horario = new TimeSpan(10, 0, 0),
            Sala_Id = 1,
            Professor_Id = 1,
            CapacidadeMaximaAlunos = 5,
            Account_Created_Id = 3,
        }, [1]);

        var evento = EventoFactory.Create(_db, new Evento
        {
            Data = TimeFunctions.HoraAtualBR(),
            Sala_Id = 1,
            CapacidadeMaximaAlunos = 999,
            DuracaoMinutos = 60,
            Evento_Tipo_Id = (int)EventoTipo.AulaZero,
            Evento_Aula = new Evento_Aula
            {
                Roteiro_Id = null,
                Professor_Id = 1,
                Turma_Id = null,
            },
        });

        var aluno = AlunoFactory.Create(_db, new Aluno { });

        var participacao = EventoFactory.CreateParticipacaoAluno(_db, evento, aluno);

        // Act
        var response = sut.FinalizarAulaZero(new FinalizarAulaZeroRequest()
        {
            Evento_Id = evento.Id,
            Observacao = "Test Aula Zero",
            Alunos = [
                new ParticipacaoAulaZeroModel
                {
                    Aluno_Id = aluno.Id,
                    Participacao_Id = participacao.Id,
                    Presente = true,
                    // Abaixo dados que devem ser gravados no aluno
                    Turma_Id = turma.Id,
                    PerfilCognitivo_Id = 1,
                    Apostila_Kit_Id = 1,
                }
            ]
        });

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success, response.Message);

        Aluno? alunoResult = _db.Alunos.Find(aluno.Id);
        Assert.NotNull(alunoResult);

        Assert.Equal(1, aluno.PerfilCognitivo_Id);
        Assert.Equal(1, aluno.Apostila_Kit_Id);
        Assert.Equal(turma.Id, aluno.Turma_Id);
    }

    //[Fact]
    //public void NaoDeve_MostrarEventosDeTurmaDesativada() {
    //    // Arrange (constructor)
    //    var startOfWeek = DateTime.Now.Date.AddDays(-(int)DateTime.Now.DayOfWeek);
    //    var endOfWeek = startOfWeek.AddDays(6);

    //    var turma = TurmaFactory.Create(_db, new Turma
    //    {
    //        DiaSemana = 5,
    //        Horario = new TimeSpan(18, 0, 0),
    //        Sala_Id = 1,
    //        Professor_Id = 1,
    //        Nome = "Test Turma",
    //        CapacidadeMaximaAlunos = 12,
    //        Unidade_Id = 1,
    //        Account_Created_Id = 3,
    //        Deactivated = TimeFunctions.HoraAtualBR(),
    //    }, [1, 2]);

    //    AlunoFactory.Create(_db, new Aluno { Turma_Id = turma.Id, PerfilCognitivo_Id = 1, Apostila_Kit_Id = 1, Apostila_Abaco_Id = 1, Apostila_AH_Id = 3 });

    //    // Act
    //    var response = sut.GetCalendario(new CalendarioRequest { IntervaloDe = startOfWeek, IntervaloAte = endOfWeek });

    //    // Assert
    //    var countEventos = response.Count;

    //    // Padrão: 1 Evento de oficina, 3 Eventos de reunião (1 geral, 1 monitoramento, 1 pedagógica)
    //    // Recorrentes: 1 Aula semanal para cada turma ativa
    //    // Eventos isolados: Aula zero, Superação, Aula extra

    //    Assert.NotNull(response);
    //    Assert.Equal(4, countEventos);

    //    var eventoTurma = response.SingleOrDefault(e => e.Evento_Tipo_Id == (int)EventoTipo.Aula);
    //    Assert.Null(eventoTurma);
    //}

    [Fact]
    public void Deve_AtualizarParticipacao() {
        // Arrange (constructor)

        var turma = TurmaFactory.Create(_db, new Turma
        {
            DiaSemana = 1,
            Horario = new TimeSpan(10, 0, 0),
            Sala_Id = 1,
            Professor_Id = 1,
            Nome = "Test Turma",
            CapacidadeMaximaAlunos = 12,
            Unidade_Id = 1,
            Account_Created_Id = 3,
        }, [1, 2]);

        Assert.NotNull(turma);

        var aluno = AlunoFactory.Create(_db, new Aluno { Turma_Id = turma.Id, PerfilCognitivo_Id = 1, Apostila_Kit_Id = 1, Apostila_Abaco_Id = 1, Apostila_AH_Id = 3 });

        var evento = EventoFactory.Create(_db, new Evento
        {
            Descricao = "Test Update",
            Data = TimeFunctions.HoraAtualBR().AddDays(1),
            Evento_Tipo_Id = (int)EventoTipo.Aula,
            DuracaoMinutos = 120,
            Sala_Id = 1,
            CapacidadeMaximaAlunos = 10,
            Account_Created_Id = 3, // Admin
            Created = TimeFunctions.HoraAtualBR(),
            Evento_Aula = new Evento_Aula
            {
                Professor_Id = (int)turma.Professor_Id!,
                Roteiro_Id = null,
                Turma_Id = turma.Id,
            },
        });

        Assert.NotNull(evento);

        var participacao = EventoFactory.CreateParticipacaoAluno(_db, evento, aluno);

        // Act
        var response = sut.UpdateParticipacao(new UpdateParticipacaoRequest
        {
            Participacao_Id = participacao.Id,
            ContatoObservacao = "Test Update Participacao",

            AlunoContactado = TimeFunctions.HoraAtualBR(),

            Deactivated = participacao.Deactivated,
            Presente = participacao.Presente,
            Observacao = participacao.Observacao,
            ReposicaoDe_Evento_Id = participacao.ReposicaoDe_Evento_Id,
            Apostila_AH_Id = participacao.Apostila_AH_Id,
            Apostila_Abaco_Id = participacao.Apostila_Abaco_Id,
            NumeroPaginaAbaco = participacao.NumeroPaginaAbaco,
            NumeroPaginaAH = participacao.NumeroPaginaAH,
        });

        // Assert

        Assert.NotNull(response);
        Assert.True(response.Success);

        Evento_Participacao_Aluno? updatedParticipacao = _db.Evento_Participacao_Alunos.Find(participacao.Id);
        Assert.NotNull(updatedParticipacao);
        Assert.Equal("Test Update Participacao", updatedParticipacao.ContatoObservacao);
    }

    [Fact]
    public void Deve_CancelarParticipacao() {
        var aluno = AlunoFactory.Create(_db, new Aluno { PerfilCognitivo_Id = 1, Apostila_Kit_Id = 1, Apostila_Abaco_Id = 1, Apostila_AH_Id = 3 });

        var evento = EventoFactory.Create(_db, new Evento
        {
            Descricao = "Test Evento Finalizar",
            Data = TimeFunctions.HoraAtualBR().AddDays(1),
            Evento_Tipo_Id = (int)EventoTipo.Oficina,
            DuracaoMinutos = 120,
            Sala_Id = 1,
            CapacidadeMaximaAlunos = 10,
            Account_Created_Id = 3, // Admin
            Created = TimeFunctions.HoraAtualBR(),
        });

        Assert.NotNull(evento);

        var participacao = EventoFactory.CreateParticipacaoAluno(_db, evento, aluno);

        Assert.NotNull(participacao);

        var response = sut.CancelarParticipacao(new()
        {
            Participacao_Id = participacao.Id,
            Observacao = "Cancelar participação",
        });

        Assert.NotNull(response);
        Assert.True(response.Success);

        var participacaoCancelada = _db.Evento_Participacao_Alunos.FirstOrDefault(p => p.Id == participacao.Id);
        Assert.NotNull(participacaoCancelada);

        // Cancelar participação insere Deactivated e Presente = False
        Assert.NotNull(participacaoCancelada.Deactivated);
        Assert.False(participacaoCancelada.Presente);
    }

    [Fact]
    public async Task Deve_SerPossivelCriarOficina() {
        // Arrange (constructor)

        // Act
        var response = await sut.Insert(new CreateEventoRequest
        {
            Sala_Id = 1,
            CapacidadeMaximaAlunos = 12,
            DuracaoMinutos = 120,
            Data = TimeFunctions.HoraAtualBR().AddDays(1),
        }, (int)EventoTipo.Oficina);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
    }

    [Fact]
    public void Deve_CancelarEvento() {
        // Arrange (constructor)

        var evento = EventoFactory.Create(_db, new Evento
        {
            Sala_Id = 1,
            CapacidadeMaximaAlunos = 999,
            DuracaoMinutos = 60,
            Data = TimeFunctions.HoraAtualBR(),
            Evento_Tipo_Id = (int)EventoTipo.AulaExtra,
            Evento_Aula = new Evento_Aula
            {
                Roteiro_Id = null,
                Professor_Id = 1,
                Turma_Id = null,
            },
        });

        Assert.NotNull(evento);

        // Act

        var response = sut.Cancelar(new CancelarEventoRequest
        {
            Id = evento.Id,
            Observacao = "Test Cancelar",
        });

        // Assert

        Assert.NotNull(response);
        Assert.True(response.Success);

        Evento? eventoCancelado = _db.Eventos.SingleOrDefault(e => e.Id == evento.Id);
        Assert.NotNull(eventoCancelado);
        Assert.NotNull(eventoCancelado.Deactivated);
    }

    [Fact]
    public void Deve_AtualizarProgressoDoAlunoNasApostilas() {
        // Arrange (constructor)
        var alunoPresente = AlunoFactory.Create(_db, new Aluno { PerfilCognitivo_Id = 1, Apostila_Kit_Id = 1, Apostila_Abaco_Id = 1, Apostila_AH_Id = 3, NumeroPaginaAbaco = 1, NumeroPaginaAH = 1 });

        var evento = EventoFactory.Create(_db, new Evento
        {
            Descricao = "Test atualizar numero página do aluno",
            Data = TimeFunctions.HoraAtualBR().AddDays(1),
            Evento_Tipo_Id = (int)EventoTipo.AulaExtra,
            DuracaoMinutos = 120,
            Sala_Id = 1,
            CapacidadeMaximaAlunos = 10,
            Account_Created_Id = 3, // Admin
            Created = TimeFunctions.HoraAtualBR(),
            Evento_Aula = new Evento_Aula
            {
                Professor_Id = 1,
                Roteiro_Id = null,
                Turma_Id = null,
            },
        });

        Assert.NotNull(evento);

        var participacaoPresente = EventoFactory.CreateParticipacaoAluno(_db, evento, alunoPresente);

        // Act
        var response = sut.Finalizar(new FinalizarEventoRequest
        {
            Evento_Id = evento.Id,
            Observacao = "Evento concluído",
            Alunos = [
                new ParticipacaoAlunoModel { Participacao_Id = participacaoPresente.Id, Presente = true, Apostila_Abaco_Id = 1, Apostila_Ah_Id = 3, NumeroPaginaAbaco = 5, NumeroPaginaAh = 5},
            ],
        });

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success, response.Message);

        var participacaoResult = _db.Evento_Participacao_Alunos.Find(participacaoPresente.Id);
        Assert.NotNull(participacaoResult);
        Assert.True(participacaoResult.Presente);
        Assert.Equal(5, participacaoResult.NumeroPaginaAbaco);
        Assert.Equal(5, participacaoResult.NumeroPaginaAH);

        var alunoResult = _db.Alunos.Find(alunoPresente.Id);
        Assert.NotNull(alunoResult);
        Assert.Equal(5, alunoResult.NumeroPaginaAbaco);
        Assert.Equal(5, alunoResult.NumeroPaginaAH);
    }
}
