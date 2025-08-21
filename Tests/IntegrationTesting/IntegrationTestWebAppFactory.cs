using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Supera_Monitor_Back.Helpers;
using Testcontainers.MsSql;
using Xunit;

namespace Tests.IntegrationTesting;

public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime {
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder) {
        builder.ConfigureTestServices(services =>
        {
            var descriptor = services
                .SingleOrDefault(s => s.ServiceType == typeof(DbContextOptions<DataContext>));

            if (descriptor is not null) {
                services.Remove(descriptor);
            }

            services.AddDbContext<DataContext>(options =>
            {
                options.UseSqlServer(_container.GetConnectionString() + ";Initial Catalog=TestDb");
            });
        });
    }

    public async Task InitializeAsync() {
        await _container.StartAsync();

        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    public new Task DisposeAsync() {
        return _container.StopAsync();
    }
}
