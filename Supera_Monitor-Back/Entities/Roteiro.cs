namespace Supera_Monitor_Back.Entities;

public partial class Roteiro {
    public int Id { get; set; }

    public string Tema { get; set; } = null!;

    public int Semana { get; set; }

    public DateTime DataInicio { get; set; }

    public DateTime DataFim { get; set; }

    public DateTime Created { get; set; }

    public int Account_Created_Id { get; set; }

    public DateTime? LastUpdated { get; set; }

    public DateTime? Deactivated { get; set; }

    public string? CorLegenda { get; set; }

    public virtual Account Account_Created { get; set; } = null!;

    public virtual ICollection<Aula> Aulas { get; set; } = new List<Aula>();

    public virtual ICollection<Evento_Aula> Evento_Aulas { get; set; } = new List<Evento_Aula>();

    public virtual ICollection<Roteiro_Material> Roteiro_Materials { get; set; } = new List<Roteiro_Material>();
}
