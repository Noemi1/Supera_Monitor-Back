namespace Supera_Monitor_Back.Entities;

public partial class Pessoa_Origem_Categoria {
    public int Id { get; set; }
    public string Nome { get; set; } = null!;
    public virtual ICollection<Pessoa_Origem> Pessoa_Origems { get; set; } = new List<Pessoa_Origem>();
}
