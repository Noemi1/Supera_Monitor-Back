namespace Supera_Monitor_Back.Models.Eventos;

public class FinalizarEventoRequest {
    public int Evento_Id { get; set; }

    public string? Observacao { get; set; }

    public List<ParticipacaoProfessorModel> Professores { get; set; } = new();
    public List<ParticipacaoAlunoModel> Alunos { get; set; } = new();
}

public class ParticipacaoProfessorModel {
    public int Participacao_Id { get; set; }

    public string? Observacao { get; set; }
    public bool Presente { get; set; }
}

public class ParticipacaoAlunoModel {
    public int Participacao_Id { get; set; }

    public string? Observacao { get; set; }
    public bool Presente { get; set; }
    public int Apostila_Abaco_Id { get; set; }
    public int NumeroPaginaAbaco { get; set; }
    public int Apostila_Ah_Id { get; set; }
    public int NumeroPaginaAh { get; set; }
}
