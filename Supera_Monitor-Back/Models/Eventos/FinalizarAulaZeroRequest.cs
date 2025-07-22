namespace Supera_Monitor_Back.Models.Eventos;

public class FinalizarAulaZeroRequest {
    public int Evento_Id { get; set; }
    public string? Observacao { get; set; }
    public List<ParticipacaoAulaZeroModel> Alunos { get; set; } = [];
}

public class ParticipacaoAulaZeroModel {
    public int Participacao_Id { get; set; }
    public bool Presente { get; set; }
    public int Aluno_Id { get; set; }
    public int Turma_Id { get; set; }
    public int PerfilCognitivo_Id { get; set; }
    public int Apostila_Kit_Id { get; set; }
}
