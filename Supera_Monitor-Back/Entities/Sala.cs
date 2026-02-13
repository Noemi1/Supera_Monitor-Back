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

public enum SalaAulaId
{
	Online1 = 1,
	Online2 = 2,
	SalaComercial = 3,
	SalaPedagogica = 4,
	SalaDiretoria = 5,
	NeuroSalaNeuronio = 6,
	NeuroSalaSinapse = 7,
	NeuroSalaAxonio = 8,
}
public enum SalaAndar
{
	Terreo = 0,
	Andar1 = 1,
	Andar2 = 2
}
