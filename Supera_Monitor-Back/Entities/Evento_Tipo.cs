namespace Supera_Monitor_Back.Entities;

public partial class Evento_Tipo {
    public int Id { get; set; }

    public string Nome { get; set; } = null!;

    public virtual ICollection<Evento> Eventos { get; set; } = new List<Evento>();
}

public enum EventoTipo {
    Aula = 1,
    Oficina = 2,
    Superacao = 3,
    Reuniao = 4,
    AulaZero = 5,
    AulaExtra = 7,
}
