using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Models.Eventos;

namespace Supera_Monitor_Back.Helpers;

public partial class DataContext : DbContext
{
	public DataContext(DbContextOptions<DataContext> options) : base(options) { }

	public virtual DbSet<Account> Accounts { get; set; }

	public virtual DbSet<AccountList> AccountLists { get; set; }

	public virtual DbSet<AccountRefreshToken> AccountRefreshTokens { get; set; }

	public virtual DbSet<AccountRole> AccountRoles { get; set; }

	public virtual DbSet<Aluno> Aluno { get; set; }

	public virtual DbSet<Aluno_Turma_Vigencia> Aluno_Turma_Vigencia { get; set; }

	public virtual DbSet<AlunoVigenciaList> AlunoVigenciaList { get; set; }

	public virtual DbSet<AlunoChecklistItemList> AlunoChecklistItemLists { get; set; }

	public virtual DbSet<AlunoChecklistView> AlunoChecklistViews { get; set; }

	public virtual DbSet<AlunoList> AlunoList { get; set; }

	public virtual DbSet<AlunoRestricaoList> AlunoRestricaoLists { get; set; }

	public virtual DbSet<Aluno_Checklist_Item> Aluno_Checklist_Item { get; set; }

	public virtual DbSet<Aluno_Historico> Aluno_Historico { get; set; }

	public virtual DbSet<Aluno_Restricao> Aluno_Restricaos { get; set; }

	public virtual DbSet<Apostila> Apostila { get; set; }

	public virtual DbSet<ApostilaList> ApostilaLists { get; set; }

	public virtual DbSet<Apostila_Kit> Apostila_Kit { get; set; }

	public virtual DbSet<Apostila_Kit_Rel> Apostila_Kit_Rel { get; set; }

	public virtual DbSet<Apostila_Tipo> Apostila_Tipos { get; set; }

	public virtual DbSet<AspNetUser> AspNetUsers { get; set; }

	public virtual DbSet<AulaEsperaList> AulaEsperaLists { get; set; }

	public virtual DbSet<CalendarioAlunoList> CalendarioAlunoList { get; set; }

	public virtual DbSet<CalendarioEventoList> CalendarioEventoList { get; set; }

	public virtual DbSet<CalendarioProfessorList> CalendarioProfessorLists { get; set; }

	public virtual DbSet<Checklist> Checklists { get; set; }

	public virtual DbSet<Checklist_Item> Checklist_Items { get; set; }

	public virtual DbSet<Evento> Evento { get; set; }

	public virtual DbSet<Evento_Aula> Evento_Aula { get; set; }

	public virtual DbSet<Evento_Aula_PerfilCognitivo_Rel> Evento_Aula_PerfilCognitivo_Rel { get; set; }

	public virtual DbSet<Evento_Participacao_Aluno> Evento_Participacao_Aluno { get; set; }

	public virtual DbSet<Evento_Participacao_Aluno_StatusContato> Evento_Participacao_Aluno_StatusContato { get; set; }

	public virtual DbSet<Evento_Participacao_Professor> Evento_Participacao_Professor { get; set; }

	public virtual DbSet<Evento_Tipo> Evento_Tipos { get; set; }

	public virtual DbSet<Feriado> Feriado { get; set; }
	
	public virtual DbSet<FeriadoList> FeriadoList { get; set; }

	public virtual DbSet<Log> Logs { get; set; }

	public virtual DbSet<LogError> LogErrors { get; set; }

	public virtual DbSet<LogList> LogLists { get; set; }

	public virtual DbSet<PerfilCognitivo> PerfilCognitivos { get; set; }

	public virtual DbSet<Pessoa> Pessoas { get; set; }

	public virtual DbSet<Pessoa_FaixaEtaria> Pessoa_FaixaEtaria { get; set; }

	public virtual DbSet<Pessoa_Geracao> Pessoa_Geracaos { get; set; }

	public virtual DbSet<Pessoa_Origem> Pessoa_Origems { get; set; }

	public virtual DbSet<Pessoa_Origem_Canal> Pessoa_Origem_Canals { get; set; }

	public virtual DbSet<Pessoa_Origem_Categoria> Pessoa_Origem_Categoria { get; set; }

	public virtual DbSet<Pessoa_Origem_Investimento> Pessoa_Origem_Investimentos { get; set; }

	public virtual DbSet<Pessoa_Sexo> Pessoa_Sexos { get; set; }

	public virtual DbSet<Pessoa_Status> Pessoa_Statuses { get; set; }

	public virtual DbSet<Professor> Professor { get; set; }

	public virtual DbSet<ProfessorList> ProfessorList { get; set; }

	public virtual DbSet<Professor_NivelCertificacao> Professor_NivelCertificacaos { get; set; }

	public virtual DbSet<Roteiro> Roteiro { get; set; }

	public virtual DbSet<Sala> Salas { get; set; }

	public virtual DbSet<Turma> Turmas { get; set; }

	public virtual DbSet<TurmaList> TurmaLists { get; set; }

	public virtual DbSet<Turma_PerfilCognitivo_Rel> Turma_PerfilCognitivo_Rels { get; set; }

	public virtual DbSet<AlunoHistoricoList> AlunoHistoricoList { get; set; }

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		if (!optionsBuilder.IsConfigured)
		{ // Se já estiver configurado, não configura de novo (Testes realizam injeção de dependências)
			var configuration = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("AppSettings.json")
				.Build();

			var connectionString = configuration.GetConnectionString("ConnectionString");

			optionsBuilder.UseSqlServer(connectionString, options =>
			{
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
		modelBuilder.Entity<Account>(entity =>
		{
			entity.ToTable("Account");

			entity.HasIndex(e => e.Account_Created_Id, "IX_Account_Account_Created_Id");

			entity.HasIndex(e => e.Role_Id, "IX_Account_Role_Id");

			entity.Property(e => e.Role_Id).HasDefaultValueSql("((1))");

			entity.HasOne(d => d.Account_Created).WithMany(p => p.InverseAccount_Created).HasForeignKey(d => d.Account_Created_Id);

			entity.HasOne(d => d.Role).WithMany(p => p.Accounts).HasForeignKey(d => d.Role_Id);

			entity.HasMany(d => d.Aluno_Turma_Vigencia)
				.WithOne(p => p.Account)
				.HasForeignKey(d => d.Account_Id);

			entity.HasMany(d => d.Feriado)
				.WithOne(p => p.Account_Created)
				.HasForeignKey(d => d.Account_Created_Id);
		});

		modelBuilder.Entity<AccountList>(entity =>
		{
			entity
				.HasNoKey()
				.ToView("AccountList");

			entity.Property(e => e.Phone)
				.HasMaxLength(256)
				.IsUnicode(false);
		});

		modelBuilder.Entity<AccountRefreshToken>(entity =>
		{
			entity.ToTable("AccountRefreshToken");

			entity.HasIndex(e => e.Account_Id, "IX_AccountRefreshToken_Account_Id");

			entity.HasOne(d => d.Account).WithMany(p => p.AccountRefreshTokens)
				.HasForeignKey(d => d.Account_Id)
				.HasConstraintName("FK_AccountRefreshToken_Account");
		});

		modelBuilder.Entity<AccountRole>(entity =>
		{
			entity.ToTable("AccountRole");
		});

		modelBuilder.Entity<Aluno>(entity =>
		{
			entity.ToTable("Aluno");

			entity.Property(e => e.Aluno_Foto).IsUnicode(false);
			entity.Property(e => e.AspNetUsers_Created_Id)
				.HasMaxLength(128)
				.IsUnicode(false);
			entity.Property(e => e.Created).HasColumnType("datetime");
			entity.Property(e => e.Deactivated).HasColumnType("datetime");
			entity.Property(e => e.LastUpdated).HasColumnType("datetime");
			entity.Property(e => e.LoginApp)
				.HasMaxLength(250)
				.IsUnicode(false);
			entity.Property(e => e.RM)
				.HasMaxLength(6)
				.IsUnicode(false);
			entity.Property(e => e.SenhaApp)
				.HasMaxLength(250)
				.IsUnicode(false);

			entity.HasOne(d => d.Apostila_AH).WithMany(p => p.AlunoApostila_AHs)
				.HasForeignKey(d => d.Apostila_AH_Id)
				.HasConstraintName("FK_Aluno_ApostilaAH");

			entity.HasOne(d => d.Apostila_Abaco).WithMany(p => p.AlunoApostila_Abacos)
				.HasForeignKey(d => d.Apostila_Abaco_Id)
				.HasConstraintName("FK_Aluno_ApostilaAbaco");

			entity.HasOne(d => d.Apostila_Kit).WithMany(p => p.Alunos)
				.HasForeignKey(d => d.Apostila_Kit_Id)
				.HasConstraintName("FK_Aluno_Apostila_Kit");

			entity.HasOne(d => d.AulaZero).WithMany(p => p.AlunoAulasZero)
				.HasForeignKey(d => d.AulaZero_Id)
				.HasConstraintName("FK_AlunoAulaZero_Evento");

			entity.HasOne(d => d.Pessoa).WithMany(p => p.Alunos)
				.HasForeignKey(d => d.Pessoa_Id)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_Aluno_Pessoa");

			entity.HasOne(d => d.PrimeiraAula).WithMany(p => p.AlunoPrimeirasAulas)
				.HasForeignKey(d => d.PrimeiraAula_Id)
				.HasConstraintName("FK_AlunoPrimeiraAula_Evento");

			entity.HasOne(d => d.Turma).WithMany(p => p.Alunos)
				.HasForeignKey(d => d.Turma_Id)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_Aluno_Turma");

			entity.HasMany(d => d.Aluno_Turma_Vigencia)
				.WithOne(p => p.Aluno)
				.HasForeignKey(d => d.Aluno_Id);
		});

		modelBuilder.Entity<AlunoChecklistItemList>(entity =>
		{
			entity
				.HasNoKey()
				.ToView("AlunoChecklistItemList");

			entity.Property(e => e.Aluno)
				.HasMaxLength(250)
				.IsUnicode(false);
			entity.Property(e => e.Celular)
				.HasMaxLength(256)
				.IsUnicode(false);
			entity.Property(e => e.Checklist)
				.HasMaxLength(50)
				.IsUnicode(false);
			entity.Property(e => e.Checklist_Item)
				.HasMaxLength(250)
				.IsUnicode(false);
			entity.Property(e => e.CorLegenda)
				.HasMaxLength(20)
				.IsUnicode(false);
			entity.Property(e => e.DataFinalizacao).HasColumnType("datetime");
			entity.Property(e => e.Email)
				.HasMaxLength(250)
				.IsUnicode(false);
			entity.Property(e => e.LinkGrupo).IsUnicode(false);
			entity.Property(e => e.Observacoes).IsUnicode(false);
			entity.Property(e => e.Prazo).HasColumnType("date");
			entity.Property(e => e.Turma)
				.HasMaxLength(100)
				.IsUnicode(false);
		});

		modelBuilder.Entity<AlunoChecklistView>(entity =>
		{
			entity
				.HasNoKey()
				.ToView("AlunoChecklistView");

			entity.Property(e => e.DataFinalizacao).HasColumnType("datetime");
			entity.Property(e => e.Nome)
				.HasMaxLength(250)
				.IsUnicode(false);
			entity.Property(e => e.Observacoes).IsUnicode(false);
			entity.Property(e => e.Prazo).HasColumnType("date");
		});

		modelBuilder.Entity<AlunoList>(entity =>
		{
			entity
				.HasNoKey()
				.ToView("AlunoList");
		});

		modelBuilder.Entity<AlunoVigenciaList>(entity =>
		{
			entity
				.HasNoKey()
				.ToView("AlunoVigenciaList");

			entity.Property(e => e.Aluno)
				.HasMaxLength(50)
				.IsUnicode(false);

			entity.Property(e => e.Turma)
				.HasMaxLength(50)
				.IsUnicode(false);

			entity.Property(e => e.Professor)
				.HasMaxLength(50)
				.IsUnicode(false);

			entity.Property(e => e.Account)
				.HasMaxLength(50)
				.IsUnicode(false);

			entity.Property(e => e.Aluno_Id)
				.HasMaxLength(128)
				.IsUnicode(false);

			entity.Property(e => e.Turma_Id)
				.HasMaxLength(128)
				.IsUnicode(false);

			entity.Property(e => e.Professor_Id)
				.HasMaxLength(128)
				.IsUnicode(false);

			entity.Property(e => e.Account_Id)
				.HasMaxLength(128)
				.IsUnicode(false);

			entity.Property(e => e.DataInicioVigencia)
				.HasColumnType("datetime");

			entity.Property(e => e.DataFimVigencia)
				.HasColumnType("datetime");
		});

		modelBuilder.Entity<AlunoRestricaoList>(entity =>
		{
			entity
				.HasNoKey()
				.ToView("AlunoRestricaoList");

			entity.Property(e => e.Created).HasColumnType("datetime");
			entity.Property(e => e.Deactivated).HasColumnType("datetime");
			entity.Property(e => e.Descricao)
				.HasMaxLength(250)
				.IsUnicode(false);
			entity.Property(e => e.Id).ValueGeneratedOnAdd();
		});

		modelBuilder.Entity<Aluno_Checklist_Item>(entity =>
		{
			entity.ToTable("Aluno_Checklist_Item");

			entity.Property(e => e.DataFinalizacao).HasColumnType("datetime");
			entity.Property(e => e.Observacoes).IsUnicode(false);
			entity.Property(e => e.Prazo).HasColumnType("date");
			entity.Property(e => e.Evento_Id).HasColumnType("int");

			entity.HasOne(d => d.Account_Finalizacao).WithMany(p => p.Aluno_Checklist_Items)
				.HasForeignKey(d => d.Account_Finalizacao_Id)
				.HasConstraintName("FK_Aluno_Checklist_Item_Account_Finalizacao");

			entity.HasOne(d => d.Aluno).WithMany(p => p.Aluno_Checklist_Items)
				.HasForeignKey(d => d.Aluno_Id)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_Aluno_Checklist_Item_Aluno");

			entity.HasOne(d => d.Checklist_Item).WithMany(p => p.Aluno_Checklist_Items)
				.HasForeignKey(d => d.Checklist_Item_Id)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_Aluno_Checklist_Item_Checklist_Item");

			entity.HasOne(d => d.Evento)
				.WithMany(p => p.Aluno_Checklist_Item)
				.HasForeignKey(d => d.Evento_Id)
				.IsRequired(false);
		});


		modelBuilder.Entity<Aluno_Historico>(entity =>
		{
			entity.ToTable("Aluno_Historico");

			entity.Property(e => e.Data).HasColumnType("datetime");
			entity.Property(e => e.Descricao).IsUnicode(false);

			entity.HasOne(d => d.Account)
				.WithMany(p => p.Aluno_Historicos)
				.HasForeignKey(d => d.Account_Id)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_Aluno_Historico_Account");

			entity.HasOne(d => d.AspNetUser)
				.WithMany(p => p.Aluno_Historicos)
				.HasForeignKey(d => d.AspNetUser_Id)
				.OnDelete(DeleteBehavior.ClientSetNull);

			entity.HasOne(d => d.Aluno)
			.WithMany(p => p.Aluno_Historicos)
				.HasForeignKey(d => d.Aluno_Id)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_Aluno_Historico_Aluno");
		});

		modelBuilder.Entity<Aluno_Restricao>(entity =>
		{
			entity.ToTable("Aluno_Restricao");

			entity.Property(e => e.Created).HasColumnType("datetime");
			entity.Property(e => e.Deactivated).HasColumnType("datetime");
			entity.Property(e => e.Descricao)
				.HasMaxLength(250)
				.IsUnicode(false);

			entity.HasOne(d => d.Account_Created).WithMany(p => p.Aluno_Restricaos)
				.HasForeignKey(d => d.Account_Created_Id)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_Aluno_Restricao_Account");

			entity.HasOne(d => d.Aluno).WithMany(p => p.Aluno_Restricaos)
				.HasForeignKey(d => d.Aluno_Id)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_Aluno_Restricao_Aluno");
		});

		modelBuilder.Entity<Apostila>(entity =>
		{
			entity.ToTable("Apostila");

			entity.Property(e => e.Nome)
				.HasMaxLength(50)
				.IsUnicode(false);
		});

		modelBuilder.Entity<ApostilaList>(entity =>
		{
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

		modelBuilder.Entity<Apostila_Kit>(entity =>
		{
			entity.ToTable("Apostila_Kit");

			entity.Property(e => e.Nome)
				.HasMaxLength(50)
				.IsUnicode(false);
		});

		modelBuilder.Entity<Apostila_Kit_Rel>(entity =>
		{
			entity.ToTable("Apostila_Kit_Rel");

			entity.HasOne(d => d.Apostila).WithMany(p => p.Apostila_Kit_Rels)
				.HasForeignKey(d => d.Apostila_Id)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_Apostila_Kit_Rel_Apostila");

			entity.HasOne(d => d.Apostila_Kit).WithMany(p => p.Apostila_Kit_Rel)
				.HasForeignKey(d => d.Apostila_Kit_Id)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_Apostila_Kit_Rel_Apostila_Kit");
		});

		modelBuilder.Entity<Apostila_Tipo>(entity =>
		{
			entity.ToTable("Apostila_Tipo");

			entity.Property(e => e.Nome)
				.HasMaxLength(50)
				.IsUnicode(false);
		});

		modelBuilder.Entity<AspNetUser>(entity =>
		{
			entity.Property(e => e.Id)
				.HasMaxLength(128)
				.IsUnicode(false);
			entity.Property(e => e.Email)
				.HasMaxLength(256)
				.IsUnicode(false);
			entity.Property(e => e.Name)
				.HasMaxLength(45)
				.IsUnicode(false);
			entity.Property(e => e.PhoneNumber)
				.HasMaxLength(256)
				.IsUnicode(false);
			entity.Property(e => e.UserName)
				.HasMaxLength(256)
				.IsUnicode(false);
		});

		modelBuilder.Entity<AulaEsperaList>(entity =>
		{
			entity
				.HasNoKey()
				.ToView("AulaEsperaList");

			entity.Property(e => e.Aluno_Foto).IsUnicode(false);
			entity.Property(e => e.Celular)
				.HasMaxLength(256)
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
				.HasMaxLength(256)
				.IsUnicode(false);
			entity.Property(e => e.Turma)
				.HasMaxLength(100)
				.IsUnicode(false);
		});

		modelBuilder.Entity<CalendarioAlunoList>(entity =>
		{
			entity.HasNoKey().ToView("CalendarioAlunoList");

			entity.Property(x => x.Id);
			entity.Property(x => x.Aluno_Id);
			entity.Property(x => x.Evento_Id);
			entity.Property(x => x.Checklist);
			entity.Property(x => x.Checklist_Id);

			entity.Property(x => x.DataNascimento)
				.HasColumnType("date");

			entity.Property(x => x.Celular);
			entity.Property(x => x.Aluno_Foto);
			entity.Property(x => x.Turma_Id);
			entity.Property(x => x.Turma);
			entity.Property(x => x.PrimeiraAula_Id);
			entity.Property(x => x.AulaZero_Id);
			entity.Property(x => x.RestricaoMobilidade);
			entity.Property(x => x.ReposicaoDe_Evento_Id);
			entity.Property(x => x.ReposicaoPara_Evento_Id);
			entity.Property(x => x.Presente);
			entity.Property(x => x.Apostila_Kit_Id);
			entity.Property(x => x.Kit);
			entity.Property(x => x.Apostila_Abaco);
			entity.Property(x => x.Apostila_AH);
			entity.Property(x => x.Apostila_Abaco_Id);
			entity.Property(x => x.Apostila_AH_Id);
			entity.Property(x => x.NumeroPaginaAbaco);
			entity.Property(x => x.NumeroPaginaAH);
			entity.Property(x => x.Observacao);

			entity.Property(x => x.Deactivated)
				.HasColumnType("datetime");

			entity.Property(x => x.AlunoContactado)
				.HasColumnType("datetime");

			entity.Property(x => x.ContatoObservacao);
			entity.Property(x => x.StatusContato_Id);
			entity.Property(x => x.PerfilCognitivo_Id);
			entity.Property(x => x.PerfilCognitivo);


			entity.Ignore(x => x.Active);
		});

		modelBuilder.Entity<CalendarioEventoList>(entity =>
		{
			entity
				.HasNoKey()
				.ToView("CalendarioEventoList");
		});

		modelBuilder.Entity<CalendarioProfessorList>(entity =>
		{
			entity
				.HasNoKey()
				.ToView("CalendarioProfessorList");

			entity.Property(e => e.CorLegenda)
				.HasMaxLength(20)
				.IsUnicode(false);
			entity.Property(e => e.Observacao).IsUnicode(false);
		});

		modelBuilder.Entity<Checklist>(entity =>
		{
			entity.ToTable("Checklist");

			entity.Property(e => e.Nome)
				.HasMaxLength(50)
				.IsUnicode(false);
		});

		modelBuilder.Entity<Checklist_Item>(entity =>
		{
			entity.ToTable("Checklist_Item");

			entity.Property(e => e.Deactivated).HasColumnType("datetime");
			entity.Property(e => e.Nome)
				.HasMaxLength(250)
				.IsUnicode(false);

			entity.HasOne(d => d.Checklist).WithMany(p => p.Checklist_Items)
				.HasForeignKey(d => d.Checklist_Id)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_Checklist_Item_Checklist");
		});

		modelBuilder.Entity<Evento>(entity =>
		{
			entity.ToTable("Evento");

			entity.Property(e => e.Created).HasColumnType("datetime");
			entity.Property(e => e.Data).HasColumnType("datetime");
			entity.Property(e => e.Deactivated).HasColumnType("datetime");
			entity.Property(e => e.Descricao)
				.HasMaxLength(250)
				.IsUnicode(false);
			entity.Property(e => e.LastUpdated)
			.HasColumnType("datetime");
			entity.Property(e => e.Observacao)
			.IsUnicode(false);

			entity.HasOne(d => d.Evento_Tipo)
				.WithMany(p => p.Eventos)
				.HasForeignKey(d => d.Evento_Tipo_Id)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_Evento_Evento_Tipo");

			entity.HasOne(d => d.ReagendamentoDe_Evento)
			.WithMany(p => p.InverseReagendamentoDe_Evento)
				.HasForeignKey(d => d.ReagendamentoDe_Evento_Id)
				.HasConstraintName("FK_Evento_ReagendamentoDe_Evento");

			entity.HasOne(d => d.Sala)
				.WithMany(p => p.Eventos)
				.HasForeignKey(d => d.Sala_Id)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_Evento_Sala");
		});

		modelBuilder.Entity<Evento_Aula>(entity =>
		{
			entity.ToTable("Evento_Aula");

			entity.Property(e => e.Id).ValueGeneratedNever();

			entity.HasOne(d => d.Evento).WithOne(p => p.Evento_Aula)
				.HasForeignKey<Evento_Aula>(d => d.Id)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_Evento_Aula_Evento");

			entity.HasOne(d => d.Professor).WithMany(p => p.Evento_Aulas)
				.HasForeignKey(d => d.Professor_Id)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_Evento_Aula_Professor");

			entity.HasOne(d => d.Roteiro)
				.WithMany(p => p.Evento_Aula)
				.HasForeignKey(d => d.Roteiro_Id)
				.HasConstraintName("FK_Evento_Aula_Roteiro");

			entity.HasOne(d => d.Turma).WithMany(p => p.Evento_Aulas)
				.HasForeignKey(d => d.Turma_Id)
				.HasConstraintName("FK_Evento_Aula_Turma");
		});

		modelBuilder.Entity<Evento_Aula_PerfilCognitivo_Rel>(entity =>
		{
			entity.ToTable("Evento_Aula_PerfilCognitivo_Rel");

			entity.HasOne(d => d.Evento_Aula).WithMany(p => p.Evento_Aula_PerfilCognitivo_Rel)
				.HasForeignKey(d => d.Evento_Aula_Id)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_Evento_Aula_PerfilCognitivo_Rel_Evento_Aula");

			entity.HasOne(d => d.PerfilCognitivo).WithMany(p => p.Evento_Aula_PerfilCognitivo_Rels)
				.HasForeignKey(d => d.PerfilCognitivo_Id)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_Evento_Aula_PerfilCognitivo_Rel_PerfilCognitivo");
		});

		modelBuilder.Entity<Evento_Participacao_Aluno>(entity =>
		{
			entity.ToTable("Evento_Participacao_Aluno");

			entity.Property(e => e.AlunoContactado).HasColumnType("datetime");
			entity.Property(e => e.ContatoObservacao).IsUnicode(false);
			entity.Property(e => e.Deactivated).HasColumnType("datetime");
			entity.Property(e => e.Observacao).IsUnicode(false);

			entity.HasOne(d => d.Aluno).WithMany(p => p.Evento_Participacao_Alunos)
				.HasForeignKey(d => d.Aluno_Id)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_Evento_Participacao_Aluno");

			entity.HasOne(d => d.Apostila_AH).WithMany(p => p.Evento_Participacao_AlunoApostila_AHs)
				.HasForeignKey(d => d.Apostila_AH_Id)
				.HasConstraintName("FK_Evento_Participacao_Aluno_ApostilaAH");

			entity.HasOne(d => d.Apostila_Abaco).WithMany(p => p.Evento_Participacao_AlunoApostila_Abacos)
				.HasForeignKey(d => d.Apostila_Abaco_Id)
				.HasConstraintName("FK_Evento_Participacao_Aluno_ApostilaAbaco");

			entity.HasOne(d => d.Evento).WithMany(p => p.Evento_Participacao_Aluno)
				.HasForeignKey(d => d.Evento_Id)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_Evento_Participacao_Aluno_Evento");

			entity.HasOne(d => d.ReposicaoDe_Evento).WithMany(p => p.Evento_Participacao_AlunoReposicaoDe_Eventos)
				.HasForeignKey(d => d.ReposicaoDe_Evento_Id)
				.HasConstraintName("FK_Evento_Participacao_Aluno_ReposicaoDe_Evento");

			entity.HasOne(d => d.StatusContato).WithMany(p => p.Evento_Participacao_Alunos)
				.HasForeignKey(d => d.StatusContato_Id)
				.HasConstraintName("FK_Evento_Participacao_Aluno_Evento_Participacao_Aluno_StatusContato");
		});

		modelBuilder.Entity<Evento_Participacao_Aluno_StatusContato>(entity =>
		{
			entity.ToTable("Evento_Participacao_Aluno_StatusContato");

			entity.Property(e => e.Descricao)
				.HasMaxLength(250)
				.IsUnicode(false);
		});

		modelBuilder.Entity<Evento_Participacao_Professor>(entity =>
		{
			entity.ToTable("Evento_Participacao_Professor");

			entity.Property(e => e.Deactivated).HasColumnType("datetime");
			entity.Property(e => e.Observacao).IsUnicode(false);

			entity.HasOne(d => d.Evento).WithMany(p => p.Evento_Participacao_Professor)
				.HasForeignKey(d => d.Evento_Id)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_Evento_Participacao_Professor_Evento");

			entity.HasOne(d => d.Professor).WithMany(p => p.Evento_Participacao_Professors)
				.HasForeignKey(d => d.Professor_Id)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_Evento_Participacao_Professor_Professor");
		});

		modelBuilder.Entity<Evento_Tipo>(entity =>
		{
			entity.ToTable("Evento_Tipo");

			entity.Property(e => e.Nome)
				.HasMaxLength(50)
				.IsUnicode(false);
		});


		modelBuilder.Entity<FeriadoList>(entity =>
		{
			entity
				.HasNoKey()
				.ToView("FeriadoList");

			entity.Property(e => e.Descricao)
				.HasMaxLength(256)
				.IsUnicode(false);
		});


		modelBuilder.Entity<Log>(entity =>
		{
			entity.ToTable("Log");

			entity.HasIndex(e => e.Account_Id, "IX_Log_Account_Id");

			entity.HasOne(d => d.Account).WithMany(p => p.Logs).HasForeignKey(d => d.Account_Id);
		});

		modelBuilder.Entity<LogError>(entity =>
		{
			entity.ToTable("LogError");
		});

		modelBuilder.Entity<LogList>(entity =>
		{
			entity
				.HasNoKey()
				.ToView("LogList");
		});

		modelBuilder.Entity<PerfilCognitivo>(entity =>
		{
			entity.ToTable("PerfilCognitivo");

			entity.Property(e => e.Descricao).IsUnicode(false);
			entity.Property(e => e.Nome)
				.HasMaxLength(50)
				.IsUnicode(false);
		});

		modelBuilder.Entity<Pessoa>(entity =>
		{
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

		modelBuilder.Entity<Pessoa_FaixaEtaria>(entity =>
		{
			entity.Property(e => e.Nome)
				.HasMaxLength(50)
				.IsUnicode(false);
		});

		modelBuilder.Entity<Pessoa_Geracao>(entity =>
		{
			entity.ToTable("Pessoa_Geracao");

			entity.Property(e => e.Id).ValueGeneratedNever();
			entity.Property(e => e.Nome)
				.HasMaxLength(50)
				.IsUnicode(false);
		});

		modelBuilder.Entity<Pessoa_Origem>(entity =>
		{
			entity.ToTable("Pessoa_Origem");

			entity.Property(e => e.Descricao)
				.HasMaxLength(8000)
				.IsUnicode(false);
			entity.Property(e => e.Investimento).HasColumnType("decimal(18, 2)");
			entity.Property(e => e.Nome)
				.HasMaxLength(250)
				.IsUnicode(false);

			entity.HasOne(d => d.Pessoa_Origem_Categoria).WithMany(p => p.Pessoa_Origems)
				.HasForeignKey(d => d.Pessoa_Origem_Categoria_Id)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_Pessoa_Origem_Pessoa_Origem_Categoria");
		});

		modelBuilder.Entity<Pessoa_Origem_Canal>(entity =>
		{
			entity.ToTable("Pessoa_Origem_Canal");

			entity.Property(e => e.Nome)
				.HasMaxLength(500)
				.IsUnicode(false);
		});

		modelBuilder.Entity<Pessoa_Origem_Categoria>(entity =>
		{
			entity.Property(e => e.Nome)
				.HasMaxLength(100)
				.IsUnicode(false);
		});

		modelBuilder.Entity<Pessoa_Origem_Investimento>(entity =>
		{
			entity.ToTable("Pessoa_Origem_Investimento");

			entity.Property(e => e.Fee).HasColumnType("decimal(18, 2)");
			entity.Property(e => e.Investimento).HasColumnType("decimal(18, 2)");
			entity.Property(e => e.InvestimentoEquipeComercial).HasColumnType("decimal(18, 2)");
			entity.Property(e => e.InvestimentoOutrasMidias).HasColumnType("decimal(18, 2)");
			entity.Property(e => e.OutrosInvestimentos).HasColumnType("decimal(18, 2)");
		});

		modelBuilder.Entity<Pessoa_Sexo>(entity =>
		{
			entity.ToTable("Pessoa_Sexo");

			entity.Property(e => e.Nome)
				.HasMaxLength(50)
				.IsUnicode(false);
		});

		modelBuilder.Entity<Pessoa_Status>(entity =>
		{
			entity.ToTable("Pessoa_Status");

			entity.Property(e => e.Nome)
				.HasMaxLength(50)
				.IsUnicode(false);
		});

		modelBuilder.Entity<Professor>(entity =>
		{
			entity.ToTable("Professor");

			entity.Property(e => e.CorLegenda)
				.HasMaxLength(20)
				.IsUnicode(false);
			entity.Property(e => e.DataInicio).HasColumnType("date");
			entity.Property(e => e.DataNascimento).HasColumnType("date");

			entity.HasOne(d => d.Account).WithMany(p => p.Professors)
				.HasForeignKey(d => d.Account_Id)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_Professor_Account");

			entity.HasOne(d => d.Professor_NivelCertificacao).WithMany(p => p.Professors)
				.HasForeignKey(d => d.Professor_NivelCertificacao_Id)
				.HasConstraintName("FK_Professor_Professor_NivelCertificacao");
		});

		modelBuilder.Entity<ProfessorList>(entity =>
		{
			entity
				.HasNoKey()
				.ToView("ProfessorList");

			entity.Property(e => e.CorLegenda)
				.HasMaxLength(20)
				.IsUnicode(false);
			entity.Property(e => e.DataInicio).HasColumnType("date");
			entity.Property(e => e.DataNascimento).HasColumnType("date");
			entity.Property(e => e.Professor_NivelCertificacao)
				.HasMaxLength(100)
				.IsUnicode(false);
			entity.Property(e => e.Telefone)
				.HasMaxLength(256)
				.IsUnicode(false);
		});

		modelBuilder.Entity<Professor_NivelCertificacao>(entity =>
		{
			entity.ToTable("Professor_NivelCertificacao");

			entity.Property(e => e.Descricao)
				.HasMaxLength(100)
				.IsUnicode(false);
		});

		modelBuilder.Entity<Roteiro>(entity =>
		{
			entity.ToTable("Roteiro");

			entity.Property(e => e.CorLegenda)
				.HasMaxLength(50)
				.IsUnicode(false);

			entity.Property(e => e.Created)
				.HasColumnType("datetime");

			entity.Property(e => e.DataFim)
				.HasColumnType("datetime");

			entity.Property(e => e.DataInicio)
				.HasColumnType("datetime");

			entity.Property(e => e.Deactivated)
				.HasColumnType("datetime");

			entity.Property(e => e.LastUpdated)
				.HasColumnType("datetime");

			entity.Property(e => e.Tema)
				.HasMaxLength(250)
				.IsUnicode(false);

			entity.HasOne(d => d.Account_Created)
				.WithMany(p => p.Roteiros)
				.HasForeignKey(d => d.Account_Created_Id)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_Roteiro_Account_Created");
		});

		modelBuilder.Entity<Sala>(entity =>
		{
			entity.ToTable("Sala");

			entity.Property(e => e.Descricao)
				.HasMaxLength(250)
				.IsUnicode(false);
		});

		modelBuilder.Entity<Turma>(entity =>
		{
			entity.ToTable("Turma");

			entity.Property(e => e.LinkGrupo).IsUnicode(false);

			entity.Property(e => e.Nome)
				.HasMaxLength(100)
				.IsUnicode(false);

			entity.HasOne(d => d.Account_Created).WithMany(p => p.Turmas)
				.HasForeignKey(d => d.Account_Created_Id)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_Turma_Account");

			entity.HasOne(d => d.Professor).WithMany(p => p.Turmas)
				.HasForeignKey(d => d.Professor_Id)
				.HasConstraintName("FK_Turma_Professor");

			entity.HasOne(d => d.Sala).WithMany(p => p.Turmas)
				.HasForeignKey(d => d.Sala_Id)
				.HasConstraintName("FK_Turma_Sala");

			entity.HasMany(d => d.Aluno_Turma_Vigencia)
				.WithOne(p => p.Turma)
				.HasForeignKey(d => d.Turma_Id);
		});

		modelBuilder.Entity<TurmaList>(entity =>
		{
			entity
				.HasNoKey()
				.ToView("TurmaList");

			entity.Property(e => e.CorLegenda)
				.HasMaxLength(20)
				.IsUnicode(false);
			entity.Property(e => e.LinkGrupo).IsUnicode(false);
			entity.Property(e => e.Nome)
				.HasMaxLength(100)
				.IsUnicode(false);
		});

		modelBuilder.Entity<Turma_PerfilCognitivo_Rel>(entity =>
		{
			entity.ToTable("Turma_PerfilCognitivo_Rel");

			entity.HasOne(d => d.PerfilCognitivo).WithMany(p => p.Turma_PerfilCognitivo_Rels)
				.HasForeignKey(d => d.PerfilCognitivo_Id)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_Turma_PerfilCognitivo_Rel_PerfilCognitivo");

			entity.HasOne(d => d.Turma).WithMany(p => p.Turma_PerfilCognitivo_Rels)
				.HasForeignKey(d => d.Turma_Id)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_Turma_PerfilCognitivo_Rel_Turma");
		});

		modelBuilder.Entity<AlunoHistoricoList>(entity =>
		{
			entity
				.HasNoKey()
				.ToView("AlunoHistoricoList");

			entity.Property(e => e.Account_Id)
				.HasMaxLength(128)
				.IsUnicode(false);
			entity.Property(e => e.Data).HasColumnType("datetime");
			entity.Property(e => e.Descricao).IsUnicode(false);
		});

		OnModelCreatingPartial(modelBuilder);
	}

	partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
