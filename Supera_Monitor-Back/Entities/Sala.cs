namespace Supera_Monitor_Back.Entities;

public partial class Sala {
    public int Id { get; set; }

    public int NumeroSala { get; set; }

    public int Andar { get; set; }

    public virtual ICollection<Evento> Eventos { get; set; } = new List<Evento>();

    public virtual ICollection<Turma> Turmas { get; set; } = new List<Turma>();
}
