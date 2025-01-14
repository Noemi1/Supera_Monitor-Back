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
        public virtual DbSet<AccountRefreshToken> AccountRefreshToken { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>(entity => {
                #region Tables

                entity.HasMany(x => x.AccountRefreshToken)
                .WithOne(x => x.Account)
                .HasForeignKey(x => x.Account_Id);

                #endregion

                #region Created

                entity.HasMany(x => x.Created_Account)
                    .WithOne(x => x.Account_Created)
                    .HasForeignKey(x => x.Account_Created_Id)
                    // Tive que adicionar essa linha pra funcionar.
                    // Tava gerando ciclos no relacionamento Account_Account_Created_Id
                    .OnDelete(DeleteBehavior.Restrict);
                /* Descrição do erro:
                 * A introdução da restrição FOREIGN KEY 'FK_Account_Account_Account_Created_Id' na tabela 'Account' pode causar ciclos ou vários caminhos em cascata.
                 * Especifique ON DELETE NO ACTION ou ON UPDATE NO ACTION, ou modifique outras restrições FOREIGN KEY.
                 * Não foi possí­vel criar a restrição ou o í­ndice. Consulte os erros anteriores.
                 */

                #endregion
            });
        }
    }
}
