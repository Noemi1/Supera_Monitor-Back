namespace Supera_Monitor_Back.Entities.Views;

public partial class AulaEsperaList {
    public int Id { get; set; }
    public int Aula_Id { get; set; }
    public int Aluno_Id { get; set; }
    public int? Turma_Id { get; set; }
    public string? Turma { get; set; }
    public int? Pessoa_Id { get; set; }
    public string? Aluno_Foto { get; set; }
    public string? Nome { get; set; }
    public string? Email { get; set; }
    public string? Celular { get; set; }
    public string? Telefone { get; set; }
    public DateTime? DataNascimento { get; set; }
    public string? Observacao { get; set; }
    public int? PerfilCognitivo_Id { get; set; }
    public string? PerfilCognitivo { get; set; }
}
