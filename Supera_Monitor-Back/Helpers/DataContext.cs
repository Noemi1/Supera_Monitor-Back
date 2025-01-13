using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities;

namespace Supera_Monitor_Back.Helpers {
    public class DataContext : DbContext {

        public DataContext(DbContextOptions<DataContext> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("AppSettings.json")
                .Build();

            var connectionString = configuration.GetConnectionString("ConnectionStringLocal");
            optionsBuilder.UseSqlServer(connectionString);
        }

        public virtual DbSet<Account> Account { get; set; }
    }
}
