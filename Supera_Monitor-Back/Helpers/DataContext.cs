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

        #region Tables
        public virtual DbSet<Account> Account { get; set; }
        public virtual DbSet<AccountRefreshToken> AccountRefreshToken { get; set; }
        public virtual DbSet<AccountRole> AccountRole { get; set; }
        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>(entity => {
                entity.HasMany(x => x.AccountRefreshToken)
                .WithOne(x => x.Account)
                .HasForeignKey(x => x.Account_Id);

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

                entity.Property(e => e.Role_Id).HasDefaultValue(( int )Role.Student);

                // Testing purposes: Create default account
                entity.HasData(
                    new Account {
                        Id = 1,
                        Name = "galax1y",
                        Email = "galax1y@test.com",
                        PasswordHash = "$2b$10$a46QGCAIbzhXEKJl36cD1OBQE5xMNyATdvrrfh1s/wtqTdawg2lHu", // Hashed "galax2y"
                        Phone = "123456789",
                        AcceptTerms = true,
                        Verified = DateTime.Now,
                        VerificationToken = "",
                        Created = DateTime.Now,
                    }
                );


            });

            modelBuilder.Entity<AccountRole>(entity => {
                entity.HasMany(x => x.Account)
                .WithOne(x => x.AccountRole)
                .HasForeignKey(x => x.Role_Id);

                entity.HasData(
                    new AccountRole { Id = ( int )Role.Admin, Role = "Admin" },
                    new AccountRole { Id = ( int )Role.Teacher, Role = "Teacher" },
                    new AccountRole { Id = ( int )Role.Assistant, Role = "Assistant" },
                    new AccountRole { Id = ( int )Role.Student, Role = "Student" }
                );
            });
        }
    }
}
