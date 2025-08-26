namespace Supera_Monitor_Back.Entities;

public partial class Sala {
    public int Id { get; set; }
    public string Descricao { get; set; } = null!;
    public int NumeroSala { get; set; }
    public int Andar { get; set; }
    public bool Online { get; set; }
    public virtual ICollection<Evento> Eventos { get; set; } = new List<Evento>();
    public virtual ICollection<Turma> Turmas { get; set; } = new List<Turma>();

    public bool PossuiAcessibilidade() {
        return Andar == 0 || Online == true;
    }
}
