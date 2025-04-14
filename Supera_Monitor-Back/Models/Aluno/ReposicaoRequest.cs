namespace Supera_Monitor_Back.Models.Aluno;

public class ReposicaoRequest {
    public int Aluno_Id { get; set; }
    public string? Observacao { get; set; } = string.Empty;

    public int Source_Aula_Id { get; set; }
    public int Dest_Aula_Id { get; set; }
}
