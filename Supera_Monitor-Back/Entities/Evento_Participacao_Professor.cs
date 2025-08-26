namespace Supera_Monitor_Back.Entities;

public partial class Evento_Participacao_Professor {
    public int Id { get; set; }
    public int Evento_Id { get; set; }
    public int Professor_Id { get; set; }
    public bool? Presente { get; set; }
    public string? Observacao { get; set; }
    public DateTime? Deactivated { get; set; }
    public virtual Evento Evento { get; set; } = null!;
    public virtual Professor Professor { get; set; } = null!;
}
