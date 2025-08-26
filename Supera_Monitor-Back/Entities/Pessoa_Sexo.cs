namespace Supera_Monitor_Back.Entities;

public partial class Pessoa_Sexo {
    public int Id { get; set; }
    public string Nome { get; set; } = null!;
    public virtual ICollection<Pessoa> Pessoas { get; set; } = new List<Pessoa>();
}
