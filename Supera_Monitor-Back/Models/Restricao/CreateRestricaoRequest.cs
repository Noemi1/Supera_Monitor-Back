namespace Supera_Monitor_Back.Models.Restricao;

public class CreateRestricaoRequest {
    public int Aluno_Id { get; set; }
    public string Descricao { get; set; } = null!;
}
