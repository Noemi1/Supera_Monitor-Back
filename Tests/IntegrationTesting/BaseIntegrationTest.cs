using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Supera_Monitor_Back.Helpers;
using Tests.IntegrationTesting.Scripts;
using Xunit;

namespace Tests.IntegrationTesting;

/* 
 * Olá.
 * 
 * Levar em consideração quando criar testes: Os containers são criados com base nas classes que herdam BaseIntegrationTest.
 * Então SalaService_Tests gera APENAS UM container para todos os testes dentro, consequentemente usam o mesmo banco
 * 
 * Um teste de GetAll pode ser afetado por um teste de Insert se realizado no mesmo arquivo, por exemplo.
 * Se o teste precisar ser feito em um container COMPLETAMENTE isolado, crie outro arquivo, mas usualmente é OK fazer um workaround.
 * 
 * Provavelmente, o ideal é criar um container por teste, mas instanciar MUITOS containers pode ser custoso, deve-se avaliar a criticidade do sistema sob teste.
 * Testes de integração geram confiança que o sistema funciona de verdade, então geralmente vale a pena.
 * 
 * É possível criar uma infraestrutura de testes ainda mais limpa utilizando a abordagem CQRS/Minimal APIs, mas não nesse repo (ja tem muita coisa, tarde demais =c)
 * 
 * Edit:    Acredito que isso foi resolvido no método InitializeAsync ao deletar e recriar o banco.
 *          Não testei, mas ainda acho que manter esse log acima é importante
 *          Quer testar? [] marca essa checkbox se foi resolvido =)
*/

public abstract class BaseIntegrationTest : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime {
    private readonly IServiceScope _scope;
    protected readonly IOptions<AppSettings> _appSettings;

    protected readonly DataContext _db;
    protected readonly IMapper _mapper;
    protected readonly IHttpContextAccessor _httpContextAccessor;

    protected readonly DataSeeder _seeder;

    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory) {
        _scope = factory.Services.CreateScope();
        _appSettings = _scope.ServiceProvider.GetRequiredService<IOptions<AppSettings>>();

        _db = _scope.ServiceProvider.GetRequiredService<DataContext>();
        _mapper = _scope.ServiceProvider.GetRequiredService<IMapper>();
        _httpContextAccessor = _scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();

        _seeder = new DataSeeder(_db);
    }

    public async Task InitializeAsync() {
        await _db.Database.EnsureDeletedAsync();
        await _db.Database.EnsureCreatedAsync();
        await _seeder.SeedAsync();
    }

    public Task DisposeAsync() {
        return Task.CompletedTask;
    }
}
