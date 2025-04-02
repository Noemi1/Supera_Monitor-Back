using Supera_Monitor_Back.Entities.Views;

namespace Supera_Monitor_Back.Models.Eventos;

public class Evento_Mes {
    public int Mes { get; set; }
    public List<Evento_Roteiro> Roteiros { get; set; } = new List<Evento_Roteiro>();
}

public class Evento_Roteiro {
    public int Id { get; set; }
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public int Semana { get; set; }
    public string Tema { get; set; } = string.Empty;
    public string? CorLegenda { get; set; }
    public string Account_Created { get; set; } = string.Empty;
    public int Account_Created_Id { get; set; }
    public DateTime Created { get; set; }
    public DateTime? LastUpdated { get; set; }
    public DateTime? Deactivated { get; set; }
    public List<CalendarioParticipacaoAlunoList> Aulas { get; set; } = new List<CalendarioParticipacaoAlunoList>();
}

public class Evento_Aula_Participacao {
    public int Aluno_Id { get; set; }
    public int Evento_Id { get; set; }
    public DateTime Data { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public int NumeroSala { get; set; }
    public int Andar { get; set; }
    public int Sala_Id { get; set; }
    public int Account_Created_Id { get; set; }
    public int Evento_Tipo_Id { get; set; }
    public DateTime Created { get; set; }
    public DateTime? LastUpdated { get; set; }
    public DateTime? Deactivated { get; set; }
    public int? Roteiro_Id { get; set; }
    public string? Turma { get; set; }
    public int? Turma_Id { get; set; }
    public string? Professor { get; set; }
    public int? Professor_Id { get; set; }
    public int Id { get; set; }
    public int? ReposicaoDe_Evento_Id { get; set; }
    public DateTime? ReposicaoDe_Evento { get; set; }
    public bool? Presente { get; set; }
    public int? NumeroPaginaAbaco { get; set; }
    public int? NumeroPaginaAH { get; set; }
    public string? Apostila_Abaco { get; set; }
    public string? Apostila_AH { get; set; }
    public int? Apostila_Abaco_Id { get; set; }
    public int? Apostila_AH_Id { get; set; }
    public string? Observacao { get; set; }

}

public class Evento_Aula_Aluno {
    public AlunoList Aluno { get; set; }
    public List<Evento_Mes> Meses { get; set; }
}

