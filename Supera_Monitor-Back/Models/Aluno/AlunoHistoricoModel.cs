namespace Supera_Monitor_Back.Models.Aluno;

public class AlunoHistoricoModel {
    public int Id { get; set; }
    public int Aluno_Id { get; set; }
    public string Descricao { get; set; } = null!;
    public DateTime Data { get; set; }
    public string Account_Created { get; set; } = string.Empty;
}
