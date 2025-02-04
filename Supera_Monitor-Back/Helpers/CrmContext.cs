using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities.CRM;

namespace Supera_Monitor_Back.Helpers {
    public partial class CrmContext : DbContext {
        public CrmContext(DbContextOptions<CrmContext> options) : base(options) { }

        public virtual DbSet<Pessoa> Pessoas { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Pessoa>(entity => {
                entity.ToTable("Pessoa");

                entity.HasIndex(e => e.DataEntrada, "nci_msft_1_Pessoa_8B43B109D3C2C9A67DA81ED263D69BE1");

                entity.HasIndex(e => new { e.Unidade_Id, e.Id }, "nci_wi_Pessoa_2637D113ECAC6392980E0BC099FD6F64");

                entity.HasIndex(e => new { e.Pessoa_Status_Id, e.Unidade_Id, e.DataEntrada }, "nci_wi_Pessoa_4AE8621B01EFA28C4B63DCBA4B295DF1");

                entity.HasIndex(e => new { e.Unidade_Id, e.Pessoa_Status_Id }, "nci_wi_Pessoa_76C27F5DF2B46E404BE2D676A1CB2902");

                entity.HasIndex(e => new { e.aspnetusers_Id, e.DataEntrada }, "nci_wi_Pessoa_7B2D30E57F3BE846247AAAD47696EE45");

                entity.HasIndex(e => new { e.Unidade_Id, e.DataEntrada }, "nci_wi_Pessoa_81AB81597214426079A02D89DCF11E9B");

                entity.HasIndex(e => new { e.Pessoa_Origem_Id, e.Unidade_Id, e.DataEntrada }, "nci_wi_Pessoa_C4AAB3385CA9335B7EC2984D3225CBFD");

                entity.HasIndex(e => new { e.Pessoa_Status_Id, e.Unidade_Id }, "nci_wi_Pessoa_D1E1C08DD811538D07CCA08DAFB906D5");

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
                    .IsUnicode(false)
                    .UseCollation("Latin1_General_CI_AI");
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
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
