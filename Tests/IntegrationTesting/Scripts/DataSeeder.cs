using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Helpers;
using BC = BCrypt.Net.BCrypt;

namespace Tests.IntegrationTesting.Scripts;

public class DataSeeder(DataContext db) {
    private readonly DataContext _db = db;

    public async Task SeedAsync() {
        await SeedEventoTipo();
        await SeedStatusContato();
        await SeedPerfilCognitivo();
        await SeedAccountRoles();
        await SeedAccountsAndProfessors();
        await SeedSalas();
        await SeedApostilaTipos();
        await SeedApostilas();
        await SeedApostilaKits();
        await SeedApostilaKitRels();
        await SeedFunctions();
        await SeedViews();
    }

    private async Task SeedStatusContato() {
        await using var transaction = await _db.Database.BeginTransactionAsync();

        _db.Database.ExecuteSqlRaw("SET IDENTITY_INSERT [Evento_Participacao_Aluno_StatusContato] ON");

        var statusContatos = new List<Evento_Participacao_Aluno_StatusContato>
        {
            new() { Id = 1, Descricao = "Não compareceu"},
            new() { Id = 2, Descricao = "Aguardando retorno"},
            new() { Id = 3, Descricao = "Optou por não repor"},
            new() { Id = 4, Descricao = "Aula cancelada"},
            new() { Id = 5, Descricao = "Reposição - Agendada"},
            new() { Id = 6, Descricao = "Reposição - Realizada"},
            new() { Id = 7, Descricao = "Reposição - Desmarcada"},
            new() { Id = 8, Descricao = "Reposição - Não compareceu"},
            new() { Id = 9, Descricao = "Outro"},
        };

        _db.Evento_Participacao_Aluno_StatusContato.AddRange(statusContatos);
        await _db.SaveChangesAsync();

        _db.Evento_Participacao_Aluno_StatusContato.AddRange();
    }

    private async Task SeedApostilaTipos() {
        await using var transaction = await _db.Database.BeginTransactionAsync();

        _db.Database.ExecuteSqlRaw("SET IDENTITY_INSERT [Apostila_Tipo] ON");

        var apostilaTipos = new List<Apostila_Tipo>
        {
            new() { Id = 1, Nome = "Abaco" },
            new() { Id = 2, Nome = "AH" },
        };

        _db.Apostila_Tipos.AddRange(apostilaTipos);
        await _db.SaveChangesAsync();

        _db.Database.ExecuteSqlRaw("SET IDENTITY_INSERT [Apostila_Tipo] OFF");

        await transaction.CommitAsync();
    }

    private async Task SeedApostilas() {
        await using var transaction = await _db.Database.BeginTransactionAsync();

        _db.Database.ExecuteSqlRaw("SET IDENTITY_INSERT [Apostila] ON");

        var apostilas = new List<Apostila>
        {
            new() { Id = 1, Nome = "ABACO 1", NumeroTotalPaginas = 10, Ordem = 1, Apostila_Tipo_Id = (int)ApostilaTipo.Abaco},
            new() { Id = 2, Nome = "ABACO 2", NumeroTotalPaginas = 10, Ordem = 2, Apostila_Tipo_Id = (int)ApostilaTipo.Abaco},
            new() { Id = 3, Nome = "AH 1", NumeroTotalPaginas = 10, Ordem = 1, Apostila_Tipo_Id = (int)ApostilaTipo.AH},
            new() { Id = 4, Nome = "AH 2", NumeroTotalPaginas = 10, Ordem = 2, Apostila_Tipo_Id = (int)ApostilaTipo.AH},
            new() { Id = 5, Nome = "TESTE ABACO 1", NumeroTotalPaginas = 15, Ordem = 1, Apostila_Tipo_Id = (int)ApostilaTipo.Abaco},
            new() { Id = 6, Nome = "TESTE ABACO 2", NumeroTotalPaginas = 20, Ordem = 2, Apostila_Tipo_Id = (int)ApostilaTipo.Abaco},
            new() { Id = 7, Nome = "TESTE AH 1", NumeroTotalPaginas = 25, Ordem = 1, Apostila_Tipo_Id = (int)ApostilaTipo.AH},
            new() { Id = 8, Nome = "TESTE AH 2", NumeroTotalPaginas = 30, Ordem = 2, Apostila_Tipo_Id = (int)ApostilaTipo.AH },
        };

        _db.Apostilas.AddRange(apostilas);
        await _db.SaveChangesAsync();

        _db.Database.ExecuteSqlRaw("SET IDENTITY_INSERT [Apostila] OFF");

        await transaction.CommitAsync();
    }

    private async Task SeedApostilaKits() {
        await using var transaction = await _db.Database.BeginTransactionAsync();

        _db.Database.ExecuteSqlRaw("SET IDENTITY_INSERT [Apostila_Kit] ON");

        var apostilaKits = new List<Apostila_Kit>
        {
            new() { Id = 1, Nome = "KIT A", CodigoBarras = null },
            new() { Id = 2, Nome = "KIT B", CodigoBarras = null },
        };

        _db.Apostila_Kits.AddRange(apostilaKits);
        await _db.SaveChangesAsync();

        _db.Database.ExecuteSqlRaw("SET IDENTITY_INSERT [Apostila_Kit] OFF");

        await transaction.CommitAsync();
    }

    private async Task SeedApostilaKitRels() {
        await using var transaction = await _db.Database.BeginTransactionAsync();

        _db.Database.ExecuteSqlRaw("SET IDENTITY_INSERT [Apostila_Kit_Rel] ON");


        var apostilaKitRels = new List<Apostila_Kit_Rel>
        {
            new() { Id = 1, Apostila_Id = 1, Apostila_Kit_Id = 1 },
            new() { Id = 2, Apostila_Id = 2, Apostila_Kit_Id = 1 },
            new() { Id = 3, Apostila_Id = 3, Apostila_Kit_Id = 1 },
            new() { Id = 4, Apostila_Id = 4, Apostila_Kit_Id = 1 },
            new() { Id = 5, Apostila_Id = 5, Apostila_Kit_Id = 2 },
            new() { Id = 6, Apostila_Id = 6, Apostila_Kit_Id = 2 },
            new() { Id = 7, Apostila_Id = 7, Apostila_Kit_Id = 2 },
            new() { Id = 8, Apostila_Id = 8, Apostila_Kit_Id = 2 },
        };

        _db.Apostila_Kit_Rels.AddRange(apostilaKitRels);
        await _db.SaveChangesAsync();

        _db.Database.ExecuteSqlRaw("SET IDENTITY_INSERT [Apostila_Kit_Rel] OFF");

        await transaction.CommitAsync();
    }

    private async Task SeedAccountRoles() {
        await using var transaction = await _db.Database.BeginTransactionAsync();

        _db.Database.ExecuteSqlRaw("SET IDENTITY_INSERT [AccountRole] ON");

        var roles = new List<AccountRole>
        {
            new() { Id = 1, Role = "Assistant" },
            new() { Id = 2, Role = "Teacher" },
            new() { Id = 3, Role = "Admin" }
        };

        _db.AccountRoles.AddRange(roles);
        await _db.SaveChangesAsync();

        _db.Database.ExecuteSqlRaw("SET IDENTITY_INSERT [AccountRole] OFF");

        await transaction.CommitAsync();
    }

    private async Task SeedEventoTipo() {
        await using var transaction = await _db.Database.BeginTransactionAsync();

        _db.Database.ExecuteSqlRaw("SET IDENTITY_INSERT [Evento_Tipo] ON");

        var tipos = new List<Evento_Tipo>
        {
            new() { Id = 1, Nome = "Aula" },
            new() { Id = 2, Nome = "Oficina" },
            new() { Id = 3, Nome = "Superação" },
            new() { Id = 4, Nome = "Reunião" },
            new() { Id = 5, Nome = "Aula 0" },
            new() { Id = 7, Nome = "Aula Extra" },
        };

        _db.Evento_Tipos.AddRange(tipos);
        await _db.SaveChangesAsync();

        _db.Database.ExecuteSqlRaw("SET IDENTITY_INSERT [Evento_Tipo] OFF");

        await transaction.CommitAsync();
    }

    private async Task SeedPerfilCognitivo() {
        await using var transaction = await _db.Database.BeginTransactionAsync();

        _db.Database.ExecuteSqlRaw("SET IDENTITY_INSERT [PerfilCognitivo] ON");

        var perfis = new List<PerfilCognitivo>
        {
            new() { Id = 1, Nome = "Junior 1", Descricao = "Idade entre 06 e 09 anos" },
            new() { Id = 2, Nome = "Junior 2", Descricao = "Idade entre 10 e 12 anos" },
            new() { Id = 3, Nome = "Adolescente", Descricao = "Idade entre 13 e 17 anos" },
            new() { Id = 4, Nome = "Adulto", Descricao = "Idade entre 18 e 59 anos" },
            new() { Id = 5, Nome = "60+", Descricao = "Idade entre 60 e 79 anos" },
            new() { Id = 6, Nome = "80+", Descricao = "Mais que 80 anos de idade" },
            new() { Id = 7, Nome = "Demência Diagnosticada", Descricao = "" },
            new() { Id = 8, Nome = "CCL - Comprometimento Cognitivo Leve", Descricao = "" },
            new() { Id = 9, Nome = "Alterações cognitivas relacionadas à idade", Descricao = "" },
            new() { Id = 10, Nome = "Envelhecimento Saudável", Descricao = "" },
            new() { Id = 11, Nome = "Super Idoso", Descricao = "" },
        };

        _db.PerfilCognitivos.AddRange(perfis);
        await _db.SaveChangesAsync();

        _db.Database.ExecuteSqlRaw("SET IDENTITY_INSERT [PerfilCognitivo] OFF");

        await transaction.CommitAsync();
    }

    private async Task SeedSalas() {
        await using var transaction = await _db.Database.BeginTransactionAsync();

        _db.Database.ExecuteSqlRaw("SET IDENTITY_INSERT [Sala] ON");

        var salas = new List<Sala>
        {
            // Salas no andar 0 ou online devem ser válidas p/ restrição de mobilidade
            new() { Id = 1, Andar = 999, Descricao = "Reunião Online", NumeroSala = 999, Online = true },
            new() { Id = 2, Andar = 0, Descricao = "Sala de Aula 1", NumeroSala = 1, Online = false },
            new() { Id = 3, Andar = 0, Descricao = "Sala de Aula 2", NumeroSala = 2, Online = false },

            new() { Id = 4, Andar = 1, Descricao = "Sala de Aula 11", NumeroSala = 11, Online = false },
            new() { Id = 5, Andar = 1, Descricao = "Sala de Aula 12", NumeroSala = 12, Online = false },
        };

        _db.Salas.AddRange(salas);
        await _db.SaveChangesAsync();

        _db.Database.ExecuteSqlRaw("SET IDENTITY_INSERT [Sala] OFF");

        await transaction.CommitAsync();
    }

    private async Task SeedAccountsAndProfessors() {
        await using var transaction = await _db.Database.BeginTransactionAsync();

        _db.Database.ExecuteSqlRaw("SET IDENTITY_INSERT [Account] ON");

        var accounts = new List<Account>
        {
            new() { Id = 1, Email = "seed_1@test.com", PasswordHash = BC.HashPassword("password_1"), Name = "SeedAccount_1", Phone = "(51) 98765-4321", Role_Id = (int)Role.Assistant, AcceptTerms = true, Created = TimeFunctions.HoraAtualBR(), Verified = TimeFunctions.HoraAtualBR() },
            new() { Id = 2, Email = "seed_2@test.com", PasswordHash = BC.HashPassword("password_2"), Name = "SeedProfessor_2", Phone = "(51) 87654-3210", Role_Id = (int)Role.Teacher, AcceptTerms = true, Created = TimeFunctions.HoraAtualBR(), Verified = TimeFunctions.HoraAtualBR() },
            new() { Id = 3, Email = "seed_3@test.com", PasswordHash = BC.HashPassword("password_3"), Name = "SeedProfessor_3", Phone = "(51) 76543-2109", Role_Id = (int)Role.Admin, AcceptTerms = true, Created = TimeFunctions.HoraAtualBR(), Verified = TimeFunctions.HoraAtualBR() },
            new() { Id = 4, Email = "seed_4@test.com", PasswordHash = BC.HashPassword("password_4"), Name = "SeedProfessor_4", Phone = "(51) 76543-2101", Role_Id = (int)Role.Teacher, AcceptTerms = true, Created = TimeFunctions.HoraAtualBR(), Verified = TimeFunctions.HoraAtualBR(), Deactivated = TimeFunctions.HoraAtualBR() },
        };

        _db.Accounts.AddRange(accounts);
        await _db.SaveChangesAsync();

        var professors = new List<Professor>
        {
            new() { Account_Id = 2, CorLegenda = "#ff0000", DataInicio = TimeFunctions.HoraAtualBR() },
            new() { Account_Id = 3, CorLegenda = "#00ff00", DataInicio = TimeFunctions.HoraAtualBR() },
            new() { Account_Id = 4, CorLegenda = "#0000ff", DataInicio = TimeFunctions.HoraAtualBR() },
        };

        _db.Professors.AddRange(professors);
        await _db.SaveChangesAsync();

        _db.Database.ExecuteSqlRaw("SET IDENTITY_INSERT [Account] OFF");

        await transaction.CommitAsync();
    }

    private async Task SeedFunctions() {
        var scriptsFolder = Path.Combine(
            AppContext.BaseDirectory,
            "IntegrationTesting",
            "Scripts"
        );

        // Pega todos os arquivos SQL da pasta
        var sqlFiles = Directory.GetFiles(scriptsFolder, "*.UserDefinedFunction.sql");

        foreach (var filePath in sqlFiles) {
            var script = await File.ReadAllTextAsync(filePath);

            await _db.Database.ExecuteSqlRawAsync(script);
        }
    }

    private async Task SeedViews() {
        var scriptsFolder = Path.Combine(
            AppContext.BaseDirectory,
            "IntegrationTesting",
            "Scripts"
        );

        // Pega todos os arquivos SQL da pasta
        var sqlFiles = Directory.GetFiles(scriptsFolder, "*.View.sql");

        foreach (var filePath in sqlFiles) {
            var script = await File.ReadAllTextAsync(filePath);

            await _db.Database.ExecuteSqlRawAsync(script);
        }
    }
}
