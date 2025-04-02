namespace Supera_Monitor_Back.Entities.Views;

public partial class CalendarioParticipacaoAlunoList {
    public int Id { get; set; }

    public int Aluno_Id { get; set; }

    public int Evento_Id { get; set; }

    public string? Descricao { get; set; }
    public bool? Presente { get; set; }

    public int? NumeroPaginaAbaco { get; set; }

    public int? NumeroPaginaAH { get; set; }

    public int? Apostila_Abaco_Id { get; set; }

    public int? Apostila_AH_Id { get; set; }

    public string? Apostila_Abaco { get; set; }

    public string? Apostila_AH { get; set; }

    public int? ReposicaoDe_Evento_Id { get; set; }

    public DateTime? Deactivated { get; set; }

    public string? Observacao { get; set; }

    public DateTime Data { get; set; }

    public int Evento_Tipo_Id { get; set; }

    public int Sala_Id { get; set; }
    public int NumeroSala { get; set; }
    public int Andar { get; set; }

    public int DuracaoMinutos { get; set; }

    public bool Finalizado { get; set; }

    public int? ReagendamentoDe_Evento_Id { get; set; }

    public int Professor_Id { get; set; }

    public string Professor { get; set; } = null!;

    public int? Turma_Id { get; set; }

    public string? Turma { get; set; }

    public int CapacidadeMaximaAlunos { get; set; }

    public int? Roteiro_Id { get; set; }
}