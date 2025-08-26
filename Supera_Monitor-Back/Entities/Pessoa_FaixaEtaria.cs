namespace Supera_Monitor_Back.Entities;

public partial class Pessoa_FaixaEtaria {
    public int Id { get; set; }

    public string Nome { get; set; } = null!;

    public bool Ativo { get; set; }

    public virtual ICollection<Pessoa> Pessoas { get; set; } = new List<Pessoa>();
}
