namespace Supera_Monitor_Back.Entities.Views;

public partial class CalendarioProfessorList {
    public int Professor_Id { get; set; }

    public string? Nome { get; set; }

    public string CorLegenda { get; set; } = null!;

    public int Account_Id { get; set; }

    public bool? Presente { get; set; }

    public string? Observacao { get; set; }

    public int Evento_Id { get; set; }
}
