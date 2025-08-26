namespace Supera_Monitor_Back.Entities.Views;

public class AlunoHistoricoList {
    public int Id { get; set; }
    public DateTime Data { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public int Aluno_Id { get; set; }
    public string? Account_Id { get; set; } // Esse pode ser chave estrangeira da AspNetUsers ou da Account
    public string? Account { get; set; }
}
