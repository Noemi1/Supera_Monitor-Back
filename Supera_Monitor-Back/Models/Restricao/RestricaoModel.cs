namespace Supera_Monitor_Back.Models.Restricao;

public class RestricaoModel {
    public int Id { get; set; }
    public string Descricao { get; set; } = null!;
    public int Aluno_Id { get; set; }
    public string Aluno { get; set; } = string.Empty;
    public int Account_Created_Id { get; set; }
    public DateTime Created { get; set; }
    public DateTime? Deactivated { get; set; }
}
