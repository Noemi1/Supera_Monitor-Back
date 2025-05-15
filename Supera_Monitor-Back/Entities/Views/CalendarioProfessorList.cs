namespace Supera_Monitor_Back.Entities.Views;

public partial class CalendarioProfessorList {
    public int? Id { get; set; }

    public int Evento_Id { get; set; }

    public int Professor_Id { get; set; }

    public int Account_Id { get; set; }

    public string? Nome { get; set; }

    public string? Telefone { get; set; }

    public string CorLegenda { get; set; } = null!;

    public bool? Presente { get; set; }

    public string? Observacao { get; set; }

    public TimeSpan? ExpedienteInicio { get; set; }

    public TimeSpan? ExpedienteFim { get; set; }
}
