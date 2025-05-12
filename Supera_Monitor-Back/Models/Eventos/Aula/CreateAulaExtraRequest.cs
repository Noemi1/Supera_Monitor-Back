namespace Supera_Monitor_Back.Models.Eventos.Aula;

public class CreateAulaExtraRequest {
    public DateTime Data { get; set; }
    public string? Descricao { get; set; }
    public string? Observacao { get; set; }
    public int Sala_Id { get; set; }
    public int Professor_Id { get; set; }
    public int DuracaoMinutos { get; set; }

    public int Roteiro_Id { get; set; }
    public int CapacidadeMaximaAlunos { get; set; }

    public List<ReposicaoAlunoModel> Alunos { get; set; } = new List<ReposicaoAlunoModel>();
    public List<int> PerfilCognitivo { get; set; } = new List<int>();

    public int? ReagendamentoDe_Evento_Id { get; set; }
}

public class ReposicaoAlunoModel {
    public int Aluno_Id { get; set; }
    public int Evento_Id { get; set; }
}