using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;

namespace Supera_Monitor_Back.Helpers {
    public partial class DataContext : DbContext {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }

        public virtual DbSet<Account> Account { get; set; }

        public virtual DbSet<AccountList> AccountList { get; set; }

        public virtual DbSet<AccountRefreshToken> AccountRefreshToken { get; set; }

        public virtual DbSet<AccountRole> AccountRole { get; set; }

        public virtual DbSet<Aluno> Aluno { get; set; }

        public virtual DbSet<AlunoChecklistView> AlunoChecklistView { get; set; }

        public virtual DbSet<AlunoList> AlunoList { get; set; }

        public virtual DbSet<Aluno_Checklist_Item> Aluno_Checklist_Item { get; set; }

        public virtual DbSet<Aluno_Restricao> Aluno_Restricao { get; set; }

        public virtual DbSet<Aluno_Restricao_Rel> Aluno_Restricao_Rel { get; set; }

        public virtual DbSet<Apostila> Apostila { get; set; }

        public virtual DbSet<ApostilaList> ApostilaList { get; set; }

        public virtual DbSet<Apostila_Kit> Apostila_Kit { get; set; }

        public virtual DbSet<Apostila_Kit_Rel> Apostila_Kit_Rel { get; set; }

        public virtual DbSet<Apostila_Tipo> Apostila_Tipo { get; set; }

        public virtual DbSet<AspNetUser> AspNetUser { get; set; }

        public virtual DbSet<Aula> Aula { get; set; }

        public virtual DbSet<AulaEsperaList> AulaEsperaList { get; set; }

        public virtual DbSet<Aula_Aluno> Aula_Aluno { get; set; }

        public virtual DbSet<Aula_ListaEspera> Aula_ListaEspera { get; set; }

        public virtual DbSet<CalendarioAlunoList> CalendarioAlunoList { get; set; }

        public virtual DbSet<CalendarioList> CalendarioList { get; set; }

        public virtual DbSet<Checklist> Checklist { get; set; }

        public virtual DbSet<Checklist_Item> Checklist_Item { get; set; }

        public virtual DbSet<Feriado> Feriado { get; set; }

        public virtual DbSet<Jornada> Jornada { get; set; }

        public virtual DbSet<Log> Log { get; set; }

        public virtual DbSet<LogError> LogError { get; set; }

        public virtual DbSet<LogList> LogList { get; set; }

        public virtual DbSet<PerfilCognitivo> PerfilCognitivo { get; set; }

        public virtual DbSet<Pessoa> Pessoa { get; set; }

        public virtual DbSet<Pessoa_FaixaEtaria> Pessoa_FaixaEtaria { get; set; }

        public virtual DbSet<Pessoa_Geracao> Pessoa_Geracao { get; set; }

        public virtual DbSet<Pessoa_Origem> Pessoa_Origem { get; set; }

        public virtual DbSet<Pessoa_Origem_Canal> Pessoa_Origem_Canal { get; set; }

        public virtual DbSet<Pessoa_Origem_Categoria> Pessoa_Origem_Categoria { get; set; }

        public virtual DbSet<Pessoa_Origem_Investimento> Pessoa_Origem_Investimento { get; set; }

        public virtual DbSet<Pessoa_Sexo> Pessoa_Sexo { get; set; }

        public virtual DbSet<Pessoa_Status> Pessoa_Status { get; set; }

        public virtual DbSet<Professor> Professor { get; set; }

        public virtual DbSet<ProfessorList> ProfessorList { get; set; }

        public virtual DbSet<Professor_AgendaPedagogica> Professor_AgendaPedagogica { get; set; }

        public virtual DbSet<Professor_AgendaPedagogica_Rel> Professor_AgendaPedagogica_Rel { get; set; }

        public virtual DbSet<Professor_NivelCertificacao> Professor_NivelCertificacao { get; set; }

        public virtual DbSet<Sala> Sala { get; set; }

        public virtual DbSet<Turma> Turma { get; set; }

        public virtual DbSet<TurmaList> TurmaList { get; set; }

        public virtual DbSet<Turma_PerfilCognitivo_Rel> Turma_PerfilCognitivo_Rel { get; set; }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured) { // Se já estiver configurado, não configura de novo (Testes realizam injeção de dependências)
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
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>(entity => {
                entity.ToTable("Account");

                entity.HasIndex(e => e.Account_Created_Id, "IX_Account_Account_Created_Id");

                entity.HasIndex(e => e.Role_Id, "IX_Account_Role_Id");

                entity.Property(e => e.Role_Id).HasDefaultValueSql("((1))");

                entity.HasOne(d => d.Account_Created).WithMany(p => p.Created_Account).HasForeignKey(d => d.Account_Created_Id);

                entity.HasOne(d => d.Role).WithMany(p => p.Accounts).HasForeignKey(d => d.Role_Id);
            });

            modelBuilder.Entity<AccountList>(entity => {
                entity
                    .HasNoKey()
                    .ToView("AccountList");
            });

            modelBuilder.Entity<AccountRefreshToken>(entity => {
                entity.ToTable("AccountRefreshToken");

                entity.HasIndex(e => e.Account_Id, "IX_AccountRefreshToken_Account_Id");

                entity.HasOne(d => d.Account).WithMany(p => p.AccountRefreshToken).HasForeignKey(d => d.Account_Id);
            });

            modelBuilder.Entity<AccountRole>(entity => {
                entity.ToTable("AccountRole");
            });

            modelBuilder.Entity<Aluno>(entity => {
                entity.ToTable("Aluno");

                entity.Property(e => e.Aluno_Foto).IsUnicode(false);
                entity.Property(e => e.AspNetUsers_Created_Id)
                    .HasMaxLength(128)
                    .IsUnicode(false);
                entity.Property(e => e.Created).HasColumnType("datetime");
                entity.Property(e => e.DataFimVigencia).HasColumnType("date");
                entity.Property(e => e.DataInicioVigencia).HasColumnType("date");
                entity.Property(e => e.Deactivated).HasColumnType("datetime");
                entity.Property(e => e.LastUpdated).HasColumnType("datetime");

                entity.HasOne(d => d.Apostila_Kit).WithMany(p => p.Alunos)
                    .HasForeignKey(d => d.Apostila_Kit_Id)
                    .HasConstraintName("FK_Aluno_Apostila_Kit");

                entity.HasOne(d => d.Turma).WithMany(p => p.Alunos)
                    .HasForeignKey(d => d.Turma_Id)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Aluno_Turma");
            });

            modelBuilder.Entity<AlunoChecklistView>(entity => {
                entity
                    .HasNoKey()
                    .ToView("AlunoChecklistView");

                entity.Property(e => e.DataFinalizacao).HasColumnType("datetime");
                entity.Property(e => e.Nome)
                    .HasMaxLength(50)
                    .IsUnicode(false);
                entity.Property(e => e.Prazo).HasColumnType("date");
            });

            modelBuilder.Entity<AlunoList>(entity => {
                entity
                    .HasNoKey()
                    .ToView("AlunoList");

                entity.Property(e => e.Apostila_AH)
                    .HasMaxLength(50)
                    .IsUnicode(false);
                entity.Property(e => e.Apostila_Abaco)
                    .HasMaxLength(50)
                    .IsUnicode(false);
                entity.Property(e => e.AspNetUsers_Created)
                    .HasMaxLength(45)
                    .IsUnicode(false);
                entity.Property(e => e.AspNetUsers_Created_Id)
                    .HasMaxLength(128)
                    .IsUnicode(false);
                entity.Property(e => e.Celular)
                    .HasMaxLength(50)
                    .IsUnicode(false);
                entity.Property(e => e.Checklist)
                    .HasMaxLength(50)
                    .IsUnicode(false);
                entity.Property(e => e.Created).HasColumnType("datetime");
                entity.Property(e => e.DataFimVigencia).HasColumnType("date");
                entity.Property(e => e.DataInicioVigencia).HasColumnType("date");
                entity.Property(e => e.DataNascimento).HasColumnType("date");
                entity.Property(e => e.Deactivated).HasColumnType("datetime");
                entity.Property(e => e.Email)
                    .HasMaxLength(250)
                    .IsUnicode(false);
                entity.Property(e => e.Endereco)
                    .HasMaxLength(250)
                    .IsUnicode(false);
                entity.Property(e => e.Kit)
                    .HasMaxLength(50)
                    .IsUnicode(false);
                entity.Property(e => e.LastUpdated).HasColumnType("datetime");
                entity.Property(e => e.Nome)
                    .HasMaxLength(250)
                    .IsUnicode(false);
                entity.Property(e => e.Observacao)
                    .HasMaxLength(8000)
                    .IsUnicode(false);
                entity.Property(e => e.Pessoa_Sexo)
                    .HasMaxLength(50)
                    .IsUnicode(false);
                entity.Property(e => e.Telefone)
                    .HasMaxLength(50)
                    .IsUnicode(false);
                entity.Property(e => e.Turma)
                    .HasMaxLength(100)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Aluno_Checklist_Item>(entity => {
                entity.ToTable("Aluno_Checklist_Item");

                entity.Property(e => e.DataFinalizacao).HasColumnType("datetime");
                entity.Property(e => e.Prazo).HasColumnType("date");

                entity.HasOne(d => d.Account_Finalizacao).WithMany(p => p.Aluno_Checklist_Item)
                    .HasForeignKey(d => d.Account_Finalizacao_Id)
                    .HasConstraintName("FK_Aluno_Checklist_Item_Account_Finalizacao");

                entity.HasOne(d => d.Aluno).WithMany(p => p.Aluno_Checklist_Item)
                    .HasForeignKey(d => d.Aluno_Id)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Aluno_Checklist_Item_Aluno");

                entity.HasOne(d => d.Checklist_Item).WithMany(p => p.Aluno_Checklist_Item)
                    .HasForeignKey(d => d.Checklist_Item_Id)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Aluno_Checklist_Item_Checklist_Item");
            });

            modelBuilder.Entity<Aluno_Restricao>(entity => {
                entity.ToTable("Aluno_Restricao");

                entity.Property(e => e.Restricao)
                    .HasMaxLength(250)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Aluno_Restricao_Rel>(entity => {
                entity.ToTable("Aluno_Restricao_Rel");

                entity.HasOne(d => d.Aluno).WithMany(p => p.Aluno_Restricao_Rel)
                    .HasForeignKey(d => d.Aluno_Id)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Aluno_Restricao_Rel_Aluno");

                entity.HasOne(d => d.Restricao).WithMany(p => p.Aluno_Restricao_Rel)
                    .HasForeignKey(d => d.Restricao_Id)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Aluno_Restricao_Rel_Aluno_Restricao");
            });

            modelBuilder.Entity<Apostila>(entity => {
                entity.ToTable("Apostila");

                entity.Property(e => e.Nome)
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<ApostilaList>(entity => {
                entity
                    .HasNoKey()
                    .ToView("ApostilaList");

                entity.Property(e => e.Kit)
                    .HasMaxLength(50)
                    .IsUnicode(false);
                entity.Property(e => e.Nome)
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Apostila_Kit>(entity => {
                entity.ToTable("Apostila_Kit");

                entity.Property(e => e.Nome)
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Apostila_Kit_Rel>(entity => {
                entity.ToTable("Apostila_Kit_Rel");

                entity.HasOne(d => d.Apostila).WithMany(p => p.Apostila_Kit_Rels)
                    .HasForeignKey(d => d.Apostila_Id)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Apostila_Kit_Rel_Apostila");

                entity.HasOne(d => d.Apostila_Kit).WithMany(p => p.Apostila_Kit_Rels)
                    .HasForeignKey(d => d.Apostila_Kit_Id)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Apostila_Kit_Rel_Apostila_Kit");
            });

            modelBuilder.Entity<Apostila_Tipo>(entity => {
                entity.ToTable("Apostila_Tipo");

                entity.Property(e => e.Nome)
                    .HasMaxLength(50)
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

            modelBuilder.Entity<Aula>(entity => {
                entity.HasKey(e => e.Id).HasName("PK_Turma_Aula");

                entity.ToTable("Aula");

                entity.Property(e => e.Created).HasColumnType("datetime");
                entity.Property(e => e.Data).HasColumnType("datetime");
                entity.Property(e => e.Deactivated).HasColumnType("datetime");
                entity.Property(e => e.LastUpdated).HasColumnType("datetime");
                entity.Property(e => e.Observacao).IsUnicode(false);

                entity.HasOne(d => d.Account_Created).WithMany(p => p.Aula)
                    .HasForeignKey(d => d.Account_Created_Id)
                    .HasConstraintName("FK_Aula_Account");

                entity.HasOne(d => d.Professor).WithMany(p => p.Aula)
                    .HasForeignKey(d => d.Professor_Id)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Aula_Professor");

                entity.HasOne(d => d.ReposicaoDe_Aula).WithMany(p => p.InverseReposicaoDe_Aula)
                    .HasForeignKey(d => d.ReposicaoDe_Aula_Id)
                    .HasConstraintName("FK_Aula_ReposicaoDe");

                entity.HasOne(d => d.Sala).WithMany(p => p.Aulas)
                    .HasForeignKey(d => d.Sala_Id)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Aula_Sala");

                entity.HasOne(d => d.Turma).WithMany(p => p.Aulas)
                    .HasForeignKey(d => d.Turma_Id)
                    .HasConstraintName("FK_Aula_Turma");
            });

            modelBuilder.Entity<AulaEsperaList>(entity => {
                entity
                    .HasNoKey()
                    .ToView("AulaEsperaList");

                entity.Property(e => e.Aluno_Foto).IsUnicode(false);
                entity.Property(e => e.Celular)
                    .HasMaxLength(50)
                    .IsUnicode(false);
                entity.Property(e => e.DataNascimento).HasColumnType("date");
                entity.Property(e => e.Email)
                    .HasMaxLength(250)
                    .IsUnicode(false);
                entity.Property(e => e.Nome)
                    .HasMaxLength(250)
                    .IsUnicode(false);
                entity.Property(e => e.Observacao)
                    .HasMaxLength(8000)
                    .IsUnicode(false);
                entity.Property(e => e.PerfilCognitivo)
                    .HasMaxLength(50)
                    .IsUnicode(false);
                entity.Property(e => e.Telefone)
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Aula_Aluno>(entity => {
                entity.HasKey(e => e.Id).HasName("PK_Turma_Aula_Aluno");

                entity.ToTable("Aula_Aluno");

                entity.Property(e => e.Observacao).IsUnicode(false);

                entity.HasOne(d => d.Aluno).WithMany(p => p.Aula_Aluno)
                    .HasForeignKey(d => d.Aluno_Id)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Aula_Aluno_Aluno");

                entity.HasOne(d => d.Apostila_AH).WithMany(p => p.Aula_AlunoApostila_AHs)
                    .HasForeignKey(d => d.Apostila_AH_Id)
                    .HasConstraintName("FK_Aula_Aluno_Apostila_AH");

                entity.HasOne(d => d.Apostila_Abaco).WithMany(p => p.Aula_AlunoApostila_Abacos)
                    .HasForeignKey(d => d.Apostila_Abaco_Id)
                    .HasConstraintName("FK_Aula_Aluno_Apostila_Abaco");

                entity.HasOne(d => d.ReposicaoDe_Aula).WithMany(p => p.Aula_Aluno)
                    .HasForeignKey(d => d.ReposicaoDe_Aula_Id)
                    .HasConstraintName("FK_Aula_Aluno_Aula");
            });

            modelBuilder.Entity<Aula_ListaEspera>(entity => {
                entity.ToTable("Aula_ListaEspera");

                entity.Property(e => e.Created).HasColumnType("datetime");

                entity.HasOne(d => d.Account_Created).WithMany(p => p.Aula_ListaEspera)
                    .HasForeignKey(d => d.Account_Created_Id)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Aula_ListaEspera_AccountCreated");

                entity.HasOne(d => d.Aluno).WithMany(p => p.Aula_ListaEspera)
                    .HasForeignKey(d => d.Aluno_Id)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Aula_ListaEspera_Aluno");

                entity.HasOne(d => d.Aula).WithMany(p => p.Aula_ListaEspera)
                    .HasForeignKey(d => d.Aula_Id)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Aula_ListaEspera_Aula");
            });

            modelBuilder.Entity<Aula_PerfilCognitivo_Rel>(entity => {
                entity.ToTable("Aula_PerfilCognitivo_Rel");

                entity.HasOne(d => d.Aula).WithMany(p => p.Aula_PerfilCognitivo_Rel)
                    .HasForeignKey(d => d.Aula_Id)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Aula_PerfilCognitivo_Rel_Aula");

                entity.HasOne(d => d.PerfilCognitivo).WithMany(p => p.Aula_PerfilCognitivo_Rel)
                    .HasForeignKey(d => d.PerfilCognitivo_Id)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Aula_PerfilCognitivo_Rel_PerfilCognitivo");
            });

            modelBuilder.Entity<CalendarioAlunoList>(entity => {
                entity
                    .HasNoKey()
                    .ToView("CalendarioAlunoList");

                entity.Property(e => e.Aluno)
                    .HasMaxLength(250)
                    .IsUnicode(false);
                entity.Property(e => e.Aluno_Foto).IsUnicode(false);
                entity.Property(e => e.Apostila_AH)
                    .HasMaxLength(50)
                    .IsUnicode(false);
                entity.Property(e => e.Apostila_Abaco)
                    .HasMaxLength(50)
                    .IsUnicode(false);
                entity.Property(e => e.Celular)
                    .HasMaxLength(50)
                    .IsUnicode(false);
                entity.Property(e => e.CheckList)
                    .HasMaxLength(50)
                    .IsUnicode(false);
                entity.Property(e => e.Kit)
                    .HasMaxLength(50)
                    .IsUnicode(false);
                entity.Property(e => e.Observacao).IsUnicode(false);
                entity.Property(e => e.Turma)
                    .HasMaxLength(100)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<CalendarioList>(entity => {
                entity
                    .HasNoKey()
                    .ToView("CalendarioList");

                entity.Property(e => e.CorLegenda)
                    .HasMaxLength(20)
                    .IsUnicode(false);
                entity.Property(e => e.Data).HasColumnType("datetime");
                entity.Property(e => e.Deactivated).HasColumnType("datetime");
                entity.Property(e => e.Observacao).IsUnicode(false);
                entity.Property(e => e.Turma)
                    .HasMaxLength(100)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Checklist>(entity => {
                entity.ToTable("Checklist");

                entity.Property(e => e.Nome)
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Checklist_Item>(entity => {
                entity.ToTable("Checklist_Item");

                entity.Property(e => e.Nome)
                    .HasMaxLength(50)
                    .IsUnicode(false);
                entity.Property(e => e.Deactivated).HasColumnType("datetime");

                entity.HasOne(d => d.Checklist).WithMany(p => p.Checklist_Items)
                    .HasForeignKey(d => d.Checklist_Id)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Checklist_Item_Checklist");
            });

            modelBuilder.Entity<Feriado>(entity => {
                entity.ToTable("Feriado");

                entity.Property(e => e.Data).HasColumnType("datetime");
                entity.Property(e => e.Descricao)
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Jornada>(entity => {
                entity.HasNoKey();

                entity.Property(e => e.DataFim).HasColumnType("datetime");
                entity.Property(e => e.DataInicio).HasColumnType("datetime");
                entity.Property(e => e.Tema)
                    .HasMaxLength(250)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Log>(entity => {
                entity.ToTable("Log");

                entity.HasIndex(e => e.Account_Id, "IX_Log_Account_Id");

                entity.HasOne(d => d.Account).WithMany(p => p.Log).HasForeignKey(d => d.Account_Id);
            });

            modelBuilder.Entity<LogError>(entity => {
                entity.ToTable("LogError");
            });

            modelBuilder.Entity<LogList>(entity => {
                entity
                    .HasNoKey()
                    .ToView("LogList");
            });

            modelBuilder.Entity<PerfilCognitivo>(entity => {
                entity.ToTable("PerfilCognitivo");

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
                entity.Property(e => e.AspNetUsers_Id)
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

                entity.HasOne(d => d.Pessoa_Status).WithMany(p => p.Pessoa)
                    .HasForeignKey(d => d.Pessoa_Status_Id)
                    .HasConstraintName("FK_Pessoa_Pessoa_Status");
            });

            modelBuilder.Entity<Pessoa_FaixaEtaria>(entity => {
                entity.Property(e => e.Id).ValueGeneratedNever();

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
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Nome)
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Pessoa_Status>(entity => {
                entity.ToTable("Pessoa_Status");
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Nome)
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Professor>(entity => {
                entity.ToTable("Professor");

                entity.Property(e => e.CorLegenda)
                    .HasMaxLength(20)
                    .IsUnicode(false);
                entity.Property(e => e.DataInicio).HasColumnType("date");
                entity.Property(e => e.DataNascimento).HasColumnType("date");

                entity.HasOne(d => d.Account).WithMany(p => p.Professor)
                    .HasForeignKey(d => d.Account_Id)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Professor_Account");

                entity.HasOne(d => d.Professor_NivelCertificacao).WithMany(p => p.Professor)
                    .HasForeignKey(d => d.Professor_NivelCertificacao_Id)
                    .HasConstraintName("FK_Professor_Professor_NivelCertificacao");
            });

            modelBuilder.Entity<ProfessorList>(entity => {
                entity
                    .HasNoKey()
                    .ToView("ProfessorList");

                entity.Property(e => e.CorLegenda)
                    .HasMaxLength(20)
                    .IsUnicode(false);
                entity.Property(e => e.DataInicio).HasColumnType("date");
                entity.Property(e => e.DataNascimento).HasColumnType("date");
            });

            modelBuilder.Entity<Professor_AgendaPedagogica>(entity => {
                entity.HasKey(e => e.Id).HasName("PK_AgendaPedagogica");

                entity.ToTable("Professor_AgendaPedagogica");

                entity.Property(e => e.Data).HasColumnType("date");
                entity.Property(e => e.Descricao)
                    .HasMaxLength(250)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Professor_AgendaPedagogica_Rel>(entity => {
                entity.ToTable("Professor_AgendaPedagogica_Rel");

                entity.HasOne(d => d.AgendaPedagogica).WithMany(p => p.Professor_AgendaPedagogica_Rel)
                    .HasForeignKey(d => d.AgendaPedagogica_Id)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Professor_AgendaPedagogica_Rel_Professor_AgendaPedagogica");

                entity.HasOne(d => d.Professor).WithMany(p => p.Professor_AgendaPedagogica_Rel)
                    .HasForeignKey(d => d.Professor_Id)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Professor_AgendaPedagogica_Rel_Professor");
            });

            modelBuilder.Entity<Professor_NivelCertificacao>(entity => {
                entity.ToTable("Professor_NivelCertificacao");

                entity.Property(e => e.Descricao)
                    .HasMaxLength(100)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Sala>(entity => {
                entity.ToTable("Sala");
            });

            modelBuilder.Entity<Turma>(entity => {
                entity.ToTable("Turma");

                entity.Property(e => e.Nome)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.HasOne(d => d.Account_Created).WithMany(p => p.Turma)
                    .HasForeignKey(d => d.Account_Created_Id)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Turma_Account");

                entity.HasOne(d => d.Professor).WithMany(p => p.Turma)
                    .HasForeignKey(d => d.Professor_Id)
                    .HasConstraintName("FK_Turma_Professor");

                entity.HasOne(d => d.Sala).WithMany(p => p.Turmas)
                    .HasForeignKey(d => d.Sala_Id)
                    .HasConstraintName("FK_Turma_Sala");
            });

            modelBuilder.Entity<TurmaList>(entity => {
                entity
                    .HasNoKey()
                    .ToView("TurmaList");

                entity.Property(e => e.CorLegenda)
                    .HasMaxLength(20)
                    .IsUnicode(false);
                entity.Property(e => e.Nome)
                    .HasMaxLength(100)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Turma_PerfilCognitivo_Rel>(entity => {
                entity.ToTable("Turma_PerfilCognitivo_Rel");

                entity.HasOne(d => d.PerfilCognitivo).WithMany(p => p.Turma_PerfilCognitivo_Rel)
                    .HasForeignKey(d => d.PerfilCognitivo_Id)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Turma_PerfilCognitivo_Rel_PerfilCognitivo");

                entity.HasOne(d => d.Turma).WithMany(p => p.Turma_PerfilCognitivo_Rel)
                    .HasForeignKey(d => d.Turma_Id)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Turma_PerfilCognitivo_Rel_Turma");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
