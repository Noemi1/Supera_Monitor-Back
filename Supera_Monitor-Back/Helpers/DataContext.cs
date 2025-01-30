using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;

namespace Supera_Monitor_Back.Helpers {
    public partial class DataContext : DbContext {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }

        public virtual DbSet<Account> Accounts { get; set; }

        public virtual DbSet<AccountList> AccountList { get; set; }

        public virtual DbSet<AccountRefreshToken> AccountRefreshTokens { get; set; }

        public virtual DbSet<AccountRole> AccountRoles { get; set; }

        public virtual DbSet<Aluno> Alunos { get; set; }

        public virtual DbSet<Log> Logs { get; set; }

        public virtual DbSet<LogError> LogErrors { get; set; }

        public virtual DbSet<LogList> LogList { get; set; }

        public virtual DbSet<Pessoa> Pessoas { get; set; }

        public virtual DbSet<Professor> Professors { get; set; }

        public virtual DbSet<Turma> Turmas { get; set; }

        public virtual DbSet<TurmaAula> TurmaAulas { get; set; }

        public virtual DbSet<TurmaAulaAluno> TurmaAulaAlunos { get; set; }

        public virtual DbSet<TurmaList> TurmaList { get; set; }

        public virtual DbSet<TurmaTipo> TurmaTipos { get; set; }

        public virtual DbSet<AlunoList> AlunoList { get; set; }

        public virtual DbSet<AulaList> AulaList { get; set; }

        public virtual DbSet<ProfessorList> ProfessorList { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("AppSettings.json")
                .Build();

            var connectionString = configuration.GetConnectionString("ConnectionString");
            optionsBuilder.UseSqlServer(connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>(entity => {
                entity.ToTable("Account");

                entity.HasIndex(e => e.Account_Created_Id, "IX_Account_Account_Created_Id");

                entity.HasIndex(e => e.Role_Id, "IX_Account_Role_Id");

                entity.Property(e => e.Account_Created_Id).HasColumnName("Account_Created_Id");
                entity.Property(e => e.Role_Id)
                    .HasDefaultValueSql("((1))")
                    .HasColumnName("Role_Id");

                entity.HasOne(d => d.Account_Created).WithMany(p => p.Created_Account).HasForeignKey(d => d.Account_Created_Id);

                entity.HasOne(d => d.Account_Role).WithMany(p => p.Accounts).HasForeignKey(d => d.Role_Id);
            });

            modelBuilder.Entity<AccountList>(entity => {
                entity
                    .HasNoKey()
                    .ToView("AccountList");

                entity.Property(e => e.Account_Created).HasColumnName("Account_Created");
                entity.Property(e => e.Account_Created_Id).HasColumnName("Account_Created_Id");
                entity.Property(e => e.Role_Id).HasColumnName("Role_Id");
            });

            modelBuilder.Entity<AccountRefreshToken>(entity => {
                entity.ToTable("AccountRefreshToken");

                entity.HasIndex(e => e.AccountId, "IX_AccountRefreshToken_Account_Id");

                entity.Property(e => e.AccountId).HasColumnName("Account_Id");

                entity.HasOne(d => d.Account).WithMany(p => p.AccountRefreshToken).HasForeignKey(d => d.AccountId);
            });

            modelBuilder.Entity<AccountRole>(entity => {
                entity.ToTable("AccountRole");
            });

            modelBuilder.Entity<Aluno>(entity => {
                entity.ToTable("Aluno");

                entity.Property(e => e.Pessoa_Id).HasColumnName("Pessoa_Id");
                entity.Property(e => e.Turma_Id).HasColumnName("Turma_Id");

                entity.HasOne(d => d.Pessoa).WithMany(p => p.Alunos)
                    .HasForeignKey(d => d.Pessoa_Id)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Aluno_Pessoa");

                entity.HasOne(d => d.Turma).WithMany(p => p.Alunos)
                    .HasForeignKey(d => d.Turma_Id)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Aluno_Turma");
            });

            modelBuilder.Entity<Log>(entity => {
                entity.ToTable("Log");

                entity.HasIndex(e => e.Account_Id, "IX_Log_Account_Id");

                entity.Property(e => e.Account_Id).HasColumnName("Account_Id");

                entity.HasOne(d => d.Account).WithMany(p => p.Logs).HasForeignKey(d => d.Account_Id);
            });

            modelBuilder.Entity<LogError>(entity => {
                entity.ToTable("LogError");
            });

            modelBuilder.Entity<LogList>(entity => {
                entity
                    .HasNoKey()
                    .ToView("LogList");

                entity.Property(e => e.Account_Id).HasColumnName("Account_Id");
            });

            modelBuilder.Entity<Pessoa>(entity => {
                entity.ToTable("Pessoa");

                entity.Property(e => e.DataNascimento).HasColumnType("date");
                entity.Property(e => e.Nome)
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Professor>(entity => {
                entity.ToTable("Professor");

                entity.Property(e => e.Account_Id).HasColumnName("Account_Id");
                entity.Property(e => e.NivelAh).HasColumnName("NivelAH");

                entity.HasOne(d => d.Account).WithMany(p => p.Professors)
                    .HasForeignKey(d => d.Account_Id)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Professor_Account");
            });

            modelBuilder.Entity<ProfessorList>(entity => {
                entity
                    .HasNoKey()
                    .ToView("ProfessorList");

                entity.Property(e => e.Account_Created).HasColumnName("Account_Created");
                entity.Property(e => e.Account_Created_Id).HasColumnName("Account_Created_Id");
                entity.Property(e => e.Account_Id).HasColumnName("Account_Id");
                entity.Property(e => e.DataInicio).HasColumnType("date");
                entity.Property(e => e.NivelAh).HasColumnName("NivelAH");
                entity.Property(e => e.Role_Id).HasColumnName("Role_Id");
            });

            modelBuilder.Entity<Turma>(entity => {
                entity.ToTable("Turma");

                entity.Property(e => e.Account_Created_Id).HasColumnName("Account_Created_Id");
                entity.Property(e => e.Professor_Id).HasColumnName("Professor_Id");
                entity.Property(e => e.Turma_Tipo_Id).HasColumnName("Turma_Tipo_Id");

                entity.HasOne(d => d.Account_Created).WithMany(p => p.Turmas)
                    .HasForeignKey(d => d.Account_Created_Id)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Turma_Account");

                entity.HasOne(d => d.Professor).WithMany(p => p.Turmas)
                    .HasForeignKey(d => d.Professor_Id)
                    .HasConstraintName("FK_Turma_Professor");

                entity.HasOne(d => d.Turma_Tipo).WithMany(p => p.Turmas)
                    .HasForeignKey(d => d.Turma_Tipo_Id)
                    .HasConstraintName("FK_Turma_Turma_Tipo");
            });

            modelBuilder.Entity<TurmaAula>(entity => {
                entity.ToTable("Turma_Aula");

                entity.Property(e => e.Data).HasColumnType("date");
                entity.Property(e => e.Professor_Id).HasColumnName("Professor_Id");
                entity.Property(e => e.Turma_Id).HasColumnName("Turma_Id");

                entity.HasOne(d => d.Professor).WithMany(p => p.Turma_Aulas)
                    .HasForeignKey(d => d.Professor_Id)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Turma_Aula_Professor");

                entity.HasOne(d => d.Turma).WithMany(p => p.Turma_Aulas)
                    .HasForeignKey(d => d.Turma_Id)
                    .HasConstraintName("FK_Turma_Aula_Turma");
            });

            modelBuilder.Entity<TurmaAulaAluno>(entity => {
                entity.ToTable("Turma_Aula_Aluno");

                entity.Property(e => e.Ah).HasColumnName("AH");
                entity.Property(e => e.Aluno_Id).HasColumnName("Aluno_Id");
                entity.Property(e => e.NumeroPaginaAh).HasColumnName("NumeroPaginaAH");
                entity.Property(e => e.Turma_Aula_Id).HasColumnName("Turma_Aula_Id");

                entity.HasOne(d => d.Aluno).WithMany(p => p.Turma_Aula_Alunos)
                    .HasForeignKey(d => d.Aluno_Id)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Turma_Aula_Aluno_Aluno");

                entity.HasOne(d => d.Turma_Aula).WithMany(p => p.Turma_Aula_Alunos)
                    .HasForeignKey(d => d.Turma_Aula_Id)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Turma_Aula_Aluno_Turma_Aula");
            });

            modelBuilder.Entity<TurmaList>(entity => {
                entity
                    .HasNoKey()
                    .ToView("TurmaList");

                entity.Property(e => e.Account_Created).HasColumnName("Account_Created");
                entity.Property(e => e.Account_Created_Id).HasColumnName("Account_Created_Id");
                entity.Property(e => e.Email_Professor).HasColumnName("Email_Professor");
                entity.Property(e => e.Nome_Professor).HasColumnName("Nome_Professor");
                entity.Property(e => e.Professor_Id).HasColumnName("Professor_Id");
                entity.Property(e => e.Telefone_Professor).HasColumnName("Telefone_Professor");
                entity.Property(e => e.Turma_Tipo)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("Turma_Tipo");
                entity.Property(e => e.Turma_Tipo_Id).HasColumnName("Turma_Tipo_Id");
            });

            modelBuilder.Entity<AlunoList>(entity => {
                entity
                    .HasNoKey()
                    .ToView("AlunoList");

                entity.Property(e => e.DataNascimento).HasColumnType("date");
                entity.Property(e => e.Nome)
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<AulaList>(entity => {
                entity
                    .HasNoKey()
                    .ToView("AulaList");

                entity.Property(e => e.Data).HasColumnType("date");
                entity.Property(e => e.Turma_Tipo)
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<TurmaTipo>(entity => {
                entity.ToTable("Turma_Tipo");

                entity.Property(e => e.Nome)
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}