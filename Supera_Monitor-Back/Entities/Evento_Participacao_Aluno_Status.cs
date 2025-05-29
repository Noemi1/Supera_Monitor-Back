namespace Supera_Monitor_Back.Entities;

public partial class Evento_Participacao_Aluno_Status {
    public int Id { get; set; }

    public string Descricao { get; set; } = null!;

    public string CorLegenda { get; set; } = null!;

    public string TextColor { get; set; } = null!;
}
