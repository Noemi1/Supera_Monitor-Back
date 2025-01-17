using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;

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

        public virtual DbSet<User> User { get; set; }

        public virtual DbSet<Log> Log { get; set; }
        public virtual DbSet<LogError> LogError { get; set; }
        #endregion

        #region Views
        public virtual DbSet<AccountList> AccountList { get; set; }
        public virtual DbSet<LogList> LogList { get; set; }
        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>(entity => {
                entity.Property(e => e.Role_Id).HasDefaultValue(( int )Role.Student);

                entity.HasMany(x => x.AccountRefreshToken)
                .WithOne(x => x.Account)
                .HasForeignKey(x => x.Account_Id);

                entity.HasMany(x => x.Logs)
                    .WithOne(x => x.Account)
                    .HasForeignKey(x => x.Account_Id);

                entity.HasMany(x => x.Created_Account)
                    .WithOne(x => x.Account_Created)
                    .HasForeignKey(x => x.Account_Created_Id)
                    // TODO: Analisar - É possível que o DeleteBehavior.Restrict não seja o comportamento desejado
                    // Porém, resolve ciclos no relacionamento Account_Account_Created_Id que não permitia a criação da tabela
                    .OnDelete(DeleteBehavior.Restrict);

                /* Descrição do erro:
                 * A introdução da restrição FOREIGN KEY 'FK_Account_Account_Account_Created_Id' na tabela 'Account' pode causar ciclos ou vários caminhos em cascata.
                 * Especifique ON DELETE NO ACTION ou ON UPDATE NO ACTION, ou modifique outras restrições FOREIGN KEY.
                 * Não foi possí­vel criar a restrição ou o í­ndice. Consulte os erros anteriores.
                 */

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

                // Seeding default roles
                entity.HasData(
                    new AccountRole { Id = ( int )Role.Admin, Role = "Admin" },
                    new AccountRole { Id = ( int )Role.Teacher, Role = "Teacher" },
                    new AccountRole { Id = ( int )Role.Assistant, Role = "Assistant" },
                    new AccountRole { Id = ( int )Role.Student, Role = "Student" }
                );
            });

            modelBuilder.Entity<User>(entity => {
                entity.HasMany(x => x.Account)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.User_Id);
            });

            #region VIEW DECLARATION

            /* 
             * Declaring a view with modelBuilder DOES NOT CREATE a view in the database
             * However, it communicates the CLI that it should not create a regular table for the view.
             * Which makes sense because a view is just a 'stored query'
             * Code-first approach problems =)
             * 
             * The view still needs to be created so the options are:
             * 1. Create the view manually on the database (drop the database = create views all over again)
             * 2. Create the view through raw SQL execution (hardcoded for safety, but adds more things to maintain)
             */

            modelBuilder.Entity<AccountList>()
                .ToView("AccountList")
                .HasKey(accList => accList.Id);

            modelBuilder.Entity<LogList>()
                .ToView("LogList")
                .HasKey(logList => logList.Id);

            #endregion
        }
    }
}
