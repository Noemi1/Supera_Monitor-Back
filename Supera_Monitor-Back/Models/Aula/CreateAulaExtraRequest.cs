using Supera_Monitor_Back.Models.Turma;

namespace Supera_Monitor_Back.Models.Aula;

public class CreateAulaExtraRequest {
    public DateTime Data { get; set; }

    public int Professor_Id { get; set; }
    public int Roteiro_Id { get; set; }
    public int Sala_Id { get; set; }

    public string? Descricao { get; set; }
    public string? Observacao { get; set; }

    public List<PerfilCognitivoModel> PerfilCognitivo { get; set; } = new List<PerfilCognitivoModel>();

    public List<int> Alunos { get; set; } = new List<int>();
}
