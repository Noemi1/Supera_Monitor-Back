namespace Supera_Monitor_Back.Models.Restricao;

public class UpdateRestricaoRequest {
    public int Id { get; set; }
    public int Aluno_Id { get; set; }
    public string Descricao { get; set; } = null!;
}
