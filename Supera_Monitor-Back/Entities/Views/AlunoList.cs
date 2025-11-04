namespace Supera_Monitor_Back.Entities.Views;

public partial class AlunoList
{
    public int Id { get; set; }
    public int Pessoa_Id { get; set; }
    public string Nome { get; set; }
    public int? Checklist_Id { get; set; }
    public string? Checklist { get; set; }
    public DateTime DataInicioVigencia { get; set; }
    public DateTime? DataFimVigencia { get; set; }
    public String? CorLegenda { get; set; }
    public DateTime? UltimaTrocaTurma { get; set; }
    public int? PrimeiraAula_Id { get; set; }
    public DateTime? PrimeiraAula { get; set; }
    public int? AulaZero_Id { get; set; }
    public DateTime? AulaZero { get; set; }
    public bool? RestricaoMobilidade { get; set; }
    public DateTime? DataNascimento { get; set; }
    public string? Celular { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public string? Observacao { get; set; }
    public int Unidade_Id { get; set; }
    public int? Turma_Id { get; set; }
    public string? Turma { get; set; }
    public int? DiaSemana { get; set; }
    public TimeSpan? Horario { get; set; }
    public int? Professor_Id { get; set; }
    public string? Professor { get; set; }
    public int? Pessoa_Sexo_Id { get; set; }
    public string? Pessoa_Sexo { get; set; }
    public int? Pessoa_Status_Id { get; set; }
    public string? Endereco { get; set; }
    public DateTime Created { get; set; }
    public DateTime? LastUpdated { get; set; }
    public DateTime? Deactivated { get; set; }
	public bool Active { get; set; }
	public string AspNetUsers_Created_Id { get; set; } = null!;
    public string? AspNetUsers_Created { get; set; }
    public int? Apostila_Kit_Id { get; set; }
    public string? Kit { get; set; }
    public string? Apostila_Abaco { get; set; }
    public int? Apostila_Abaco_Id { get; set; }
    public string? Apostila_AH { get; set; }
    public int? Apostila_AH_Id { get; set; }
    public int? NumeroPaginaAbaco { get; set; }
    public int? NumeroPaginaAH { get; set; }
    public int? PerfilCognitivo_Id { get; set; }
    public string? PerfilCognitivo { get; set; }
    public string RM { get; set; } = string.Empty;
    public string? LoginApp { get; set; }
    public string? SenhaApp { get; set; }
    public string? LinkGrupo { get; set; }
    public virtual ICollection<AlunoRestricaoList> Restricoes { get; set; } = new List<AlunoRestricaoList>();
}