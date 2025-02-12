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

        public virtual DbSet<Apostila> Apostilas { get; set; }

        public virtual DbSet<AspNetUser> AspNetUsers { get; set; }

        public virtual DbSet<CalendarioAlunoList> CalendarioAlunoList { get; set; }

        public virtual DbSet<CalendarioList> CalendarioList { get; set; }

        public virtual DbSet<Log> Logs { get; set; }

        public virtual DbSet<LogError> LogErrors { get; set; }

        public virtual DbSet<LogList> LogList { get; set; }

        public virtual DbSet<Professor> Professors { get; set; }

        public virtual DbSet<Turma> Turmas { get; set; }

        public virtual DbSet<TurmaAula> TurmaAulas { get; set; }

        public virtual DbSet<TurmaAulaAluno> TurmaAulaAlunos { get; set; }

        public virtual DbSet<TurmaList> TurmaList { get; set; }

        public virtual DbSet<TurmaTipo> TurmaTipos { get; set; }

        public virtual DbSet<AlunoList> AlunoList { get; set; }

        public virtual DbSet<AulaList> AulaList { get; set; }

        public virtual DbSet<ProfessorList> ProfessorList { get; set; }

        public virtual DbSet<Professor_NivelAH> Professor_NivelAH { get; set; }

        public virtual DbSet<Professor_NivelAbaco> Professor_NivelAbaco { get; set; }

        public virtual DbSet<Pessoa> Pessoas { get; set; }

        public virtual DbSet<Pessoa_FaixaEtaria> Pessoa_FaixaEtaria { get; set; }

        public virtual DbSet<Pessoa_Geracao> Pessoa_Geracoes { get; set; }

        public virtual DbSet<Pessoa_Origem> Pessoa_Origems { get; set; }

        public virtual DbSet<Pessoa_Origem_Canal> Pessoa_Origem_Canals { get; set; }

        public virtual DbSet<Pessoa_Origem_Categoria> Pessoa_Origem_Categoria { get; set; }

        public virtual DbSet<Pessoa_Origem_Investimento> Pessoa_Origem_Investimentos { get; set; }

        public virtual DbSet<Pessoa_Sexo> Pessoa_Sexos { get; set; }

        public virtual DbSet<Pessoa_Status> Pessoa_Statuses { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("AppSettings.json")
                .Build();

            var connectionString = configuration.GetConnectionString("ConnectionString");

            optionsBuilder.UseSqlServer(connectionString, options => {
                options.CommandTimeout(1200); // 20 minutos
                options.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null
                );
            });
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>(entity => {
                entity.ToTable("Account");

                entity.HasIndex(e => e.Account_Created_Id, "IX_Account_Account_Created_Id");

                entity.HasIndex(e => e.Role_Id, "IX_Account_Role_Id");

                entity.Property(e => e.Role_Id).HasDefaultValueSql("((1))");

                entity.HasOne(d => d.Account_Created).WithMany(p => p.Created_Account).HasForeignKey(d => d.Account_Created_Id);

                entity.HasOne(d => d.Account_Role).WithMany(p => p.Accounts).HasForeignKey(d => d.Role_Id);
            });

            modelBuilder.Entity<AccountList>(entity => {
                entity
                    .HasNoKey()
                    .ToView("AccountList");
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

                entity.Property(e => e.Aluno_Foto).IsUnicode(false);
                entity.Property(e => e.Created).HasColumnType("datetime");
                entity.Property(e => e.Deactivated).HasColumnType("datetime");
                entity.Property(e => e.LastUpdated).HasColumnType("datetime");
                entity.Property(e => e.AspNetUsers_Created_Id)
                    .HasMaxLength(128)
                    .IsUnicode(false);

                entity.HasOne(d => d.Turma).WithMany(p => p.Alunos)
                    .HasForeignKey(d => d.Turma_Id)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Aluno_Turma");
            });

            modelBuilder.Entity<Apostila>(entity => {
                entity.ToTable("Apostila");

                entity.Property(e => e.Nome)
                    .HasMaxLength(250)
                    .IsUnicode(false);
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

            modelBuilder.Entity<Professor>(entity => {
                entity.ToTable("Professor");

                entity.Property(e => e.Account_Id).HasColumnName("Account_Id");

                entity.Property(e => e.CorLegenda)
                    .HasMaxLength(20)
                    .IsUnicode(false);
                entity.Property(e => e.DataInicio).HasColumnType("date");

                entity.HasOne(d => d.Account).WithMany(p => p.Professors)
                    .HasForeignKey(d => d.Account_Id)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Professor_Account");

                entity.HasOne(d => d.Professor_NivelAH).WithMany(p => p.Professors)
                    .HasForeignKey(d => d.Professor_NivelAH_Id)
                    .HasConstraintName("FK_Professor_Professor_NivelAH");

                entity.HasOne(d => d.Professor_NivelAbaco).WithMany(p => p.Professors)
                    .HasForeignKey(d => d.Professor_NivelAbaco_Id)
                    .HasConstraintName("FK_Professor_Professor_NivelAbaco");
            });

            modelBuilder.Entity<ProfessorList>(entity => {
                entity
                    .HasNoKey()
                    .ToView("ProfessorList");

                entity.Property(e => e.DataInicio).HasColumnType("date");
                entity.Property(e => e.NivelAH)
                    .HasMaxLength(100)
                    .IsUnicode(false);
                entity.Property(e => e.NivelAbaco)
                    .HasMaxLength(100)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Professor_NivelAH>(entity => {
                entity.ToTable("Professor_NivelAH");

                entity.Property(e => e.Descricao)
                    .HasMaxLength(100)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Professor_NivelAbaco>(entity => {
                entity.ToTable("Professor_NivelAbaco");

                entity.Property(e => e.Descricao)
                    .HasMaxLength(100)
                    .IsUnicode(false);
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
                entity.Property(e => e.Observacao)
                    .HasMaxLength(10)
                    .IsFixedLength();

                entity.Property(e => e.Professor_Id).HasColumnName("Professor_Id");
                entity.HasOne(d => d.Professor).WithMany(p => p.Turma_Aulas)
                    .HasForeignKey(d => d.Professor_Id)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Turma_Aula_Professor");

                entity.Property(e => e.Turma_Id).HasColumnName("Turma_Id");
                entity.HasOne(d => d.Turma).WithMany(p => p.Turma_Aulas)
                    .HasForeignKey(d => d.Turma_Id)
                    .HasConstraintName("FK_Turma_Aula_Turma");
            });

            modelBuilder.Entity<TurmaAulaAluno>(entity => {
                entity.ToTable("Turma_Aula_Aluno");

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

                entity.Property(e => e.Nome)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.Turma_Tipo)
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<AlunoList>(entity => {
                entity
                    .HasNoKey()
                    .ToView("AlunoList");

                //entity.Property(e => e.Aluno_Foto).IsUnicode(false);
                entity.Property(e => e.AspNetUsers_Created)
                    .HasMaxLength(45)
                    .IsUnicode(false);
                entity.Property(e => e.AspNetUsers_Created_Id)
                    .HasMaxLength(128)
                    .IsUnicode(false);
                entity.Property(e => e.CPF)
                    .HasMaxLength(20)
                    .IsUnicode(false);
                entity.Property(e => e.Celular)
                    .HasMaxLength(50)
                    .IsUnicode(false);
                entity.Property(e => e.Created).HasColumnType("datetime");
                entity.Property(e => e.DataCadastro).HasColumnType("datetime");
                entity.Property(e => e.DataEntrada).HasColumnType("datetime");
                entity.Property(e => e.DataNascimento).HasColumnType("date");
                entity.Property(e => e.Deactivated).HasColumnType("datetime");
                entity.Property(e => e.Email)
                    .HasMaxLength(250)
                    .IsUnicode(false);
                entity.Property(e => e.Endereco)
                    .HasMaxLength(250)
                    .IsUnicode(false);
                entity.Property(e => e.LastUpdated).HasColumnType("datetime");
                entity.Property(e => e.Nome)
                    .HasMaxLength(250)
                    .IsUnicode(false);
                entity.Property(e => e.Observacao)
                    .HasMaxLength(8000)
                    .IsUnicode(false);
                entity.Property(e => e.Pessoa_FaixaEtaria)
                    .HasMaxLength(50)
                    .IsUnicode(false);
                entity.Property(e => e.Pessoa_Geracao)
                    .HasMaxLength(50)
                    .IsUnicode(false);
                entity.Property(e => e.Pessoa_Indicou)
                    .HasMaxLength(250)
                    .IsUnicode(false);
                entity.Property(e => e.Pessoa_Origem)
                    .HasMaxLength(250)
                    .IsUnicode(false);
                entity.Property(e => e.Pessoa_Origem_Canal)
                    .HasMaxLength(500)
                    .IsUnicode(false);
                entity.Property(e => e.Pessoa_Sexo)
                    .HasMaxLength(50)
                    .IsUnicode(false);
                entity.Property(e => e.Pessoa_Status)
                    .HasMaxLength(50)
                    .IsUnicode(false);
                entity.Property(e => e.RG)
                    .HasMaxLength(20)
                    .IsUnicode(false);
                entity.Property(e => e.Telefone)
                    .HasMaxLength(50)
                    .IsUnicode(false);
                entity.Property(e => e.Turma)
                    .HasMaxLength(100)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<AulaList>(entity => {
                entity
                    .HasNoKey()
                    .ToView("AulaList");

                entity.Property(e => e.Data).HasColumnType("date");
                entity.Property(e => e.Turma)
                    .HasMaxLength(100)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<AspNetUser>(entity => {
                entity.Property(e => e.Id)
                    .HasMaxLength(128)
                    .IsUnicode(false);
                entity.Property(e => e.Email)
                    .HasMaxLength(256)
                    .IsUnicode(false);
                entity.Property(e => e.EmailSenha)
                    .HasMaxLength(45)
                    .IsUnicode(false);
                entity.Property(e => e.LockoutEndDateUtc).HasColumnType("datetime");
                entity.Property(e => e.Name)
                    .HasMaxLength(45)
                    .IsUnicode(false);
                entity.Property(e => e.PasswordHash)
                    .HasMaxLength(256)
                    .IsUnicode(false);
                entity.Property(e => e.PhoneNumber)
                    .HasMaxLength(256)
                    .IsUnicode(false);
                entity.Property(e => e.SecurityStamp)
                    .HasMaxLength(256)
                    .IsUnicode(false);
                entity.Property(e => e.UserName)
                    .HasMaxLength(256)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<TurmaTipo>(entity => {
                entity.ToTable("Turma_Tipo");

                entity.Property(e => e.Nome)
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Pessoa>(entity => {
                entity.ToTable("Pessoa");

                entity.Property(e => e.CPF)
                    .HasMaxLength(20)
                    .IsUnicode(false);
                entity.Property(e => e.Celular)
                    .HasMaxLength(50)
                    .IsUnicode(false);
                entity.Property(e => e.DataCadastro).HasColumnType("datetime");
                entity.Property(e => e.DataEntrada).HasColumnType("datetime");
                entity.Property(e => e.DataNascimento).HasColumnType("date");
                entity.Property(e => e.Email)
                    .HasMaxLength(250)
                    .IsUnicode(false);
                entity.Property(e => e.Endereco)
                    .HasMaxLength(250)
                    .IsUnicode(false);
                entity.Property(e => e.Nome)
                    .HasMaxLength(250)
                    .IsUnicode(false);
                entity.Property(e => e.Observacao)
                    .HasMaxLength(8000)
                    .IsUnicode(false);
                entity.Property(e => e.RG)
                    .HasMaxLength(20)
                    .IsUnicode(false);
                entity.Property(e => e.Telefone)
                    .HasMaxLength(50)
                    .IsUnicode(false);
                entity.Property(e => e.aspnetusers_Id)
                    .HasMaxLength(128)
                    .IsUnicode(false);

                entity.HasOne(d => d.Pessoa_FaixaEtaria).WithMany(p => p.Pessoas)
                    .HasForeignKey(d => d.Pessoa_FaixaEtaria_Id)
                    .HasConstraintName("FK_Pessoa_Pessoa_FaixaEtaria");

                entity.HasOne(d => d.Pessoa_Geracao).WithMany(p => p.Pessoas)
                    .HasForeignKey(d => d.Pessoa_Geracao_Id)
                    .HasConstraintName("FK_Pessoa_Pessoa_Geracao");

                entity.HasOne(d => d.Pessoa_Origem).WithMany(p => p.Pessoas)
                    .HasForeignKey(d => d.Pessoa_Origem_Id)
                    .HasConstraintName("FK_Pessoa_Pessoa_Origem");

                entity.HasOne(d => d.Pessoa_Sexo).WithMany(p => p.Pessoas)
                    .HasForeignKey(d => d.Pessoa_Sexo_Id)
                    .HasConstraintName("FK_Pessoa_Pessoa_Sexo");

                entity.HasOne(d => d.Pessoa_Status).WithMany(p => p.Pessoas)
                    .HasForeignKey(d => d.Pessoa_Status_Id)
                    .HasConstraintName("FK_Pessoa_Pessoa_Status");
            });

            modelBuilder.Entity<Pessoa_FaixaEtaria>(entity => {
                entity.Property(e => e.Nome)
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Pessoa_Geracao>(entity => {
                entity.ToTable("Pessoa_Geracao");

                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.Property(e => e.Nome)
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Pessoa_Origem>(entity => {
                entity.ToTable("Pessoa_Origem");

                entity.Property(e => e.Descricao)
                    .HasMaxLength(8000)
                    .IsUnicode(false);
                entity.Property(e => e.Investimento).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.Nome)
                    .HasMaxLength(250)
                    .IsUnicode(false);

                entity.HasOne(d => d.Pessoa_Origem_Categoria).WithMany(p => p.Pessoa_Origem)
                    .HasForeignKey(d => d.Pessoa_Origem_Categoria_Id)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Pessoa_Origem_Pessoa_Origem_Categoria");
            });

            modelBuilder.Entity<Pessoa_Origem_Canal>(entity => {
                entity.ToTable("Pessoa_Origem_Canal");

                entity.Property(e => e.Nome)
                    .HasMaxLength(500)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Pessoa_Origem_Categoria>(entity => {
                entity.Property(e => e.Nome)
                    .HasMaxLength(100)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Pessoa_Origem_Investimento>(entity => {
                entity.ToTable("Pessoa_Origem_Investimento");

                entity.Property(e => e.Fee).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.Investimento).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.InvestimentoEquipeComercial).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.InvestimentoOutrasMidias).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.OutrosInvestimentos).HasColumnType("decimal(18, 2)");
            });

            modelBuilder.Entity<Pessoa_Sexo>(entity => {
                entity.ToTable("Pessoa_Sexo");

                entity.Property(e => e.Nome)
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Pessoa_Status>(entity => {
                entity.ToTable("Pessoa_Status");

                entity.Property(e => e.Nome)
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<CalendarioAlunoList>(entity => {
                entity.HasNoKey()
                    .ToView("CalendarioAlunoList");
            });

            modelBuilder.Entity<CalendarioList>(entity => {
                entity
                    .HasNoKey()
                    .ToView("CalendarioList");

                entity.Property(e => e.CorLegenda)
                    .HasMaxLength(20)
                    .IsUnicode(false);
                entity.Property(e => e.Data).HasColumnType("date");
                entity.Property(e => e.Turma)
                    .HasMaxLength(100)
                    .IsUnicode(false);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
