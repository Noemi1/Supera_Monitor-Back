using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;

namespace Supera_Monitor_Back.CRM4U;

public partial class CRM4UContext : DbContext {

    public CRM4UContext(DbContextOptions<CRM4UContext> options) : base(options) { }

    public virtual DbSet<PessoaCRM> Pessoa { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
        if (!optionsBuilder.IsConfigured) { // Se já estiver configurado, não configura de novo (Testes realizam injeção de dependências)
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("AppSettings.json")
                .Build();

            var connectionString = configuration.GetConnectionString("CRM4U_ConnectionString");

            optionsBuilder.UseSqlServer(connectionString, options => {
                options.CommandTimeout(1200); // 20 minutos
                options.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null
                );
            });
        }
    }
}
