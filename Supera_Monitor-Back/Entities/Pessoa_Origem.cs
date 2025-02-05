namespace Supera_Monitor_Back.Entities;

public partial class Pessoa_Origem {
    public int Id { get; set; }

    public string Nome { get; set; } = null!;

    public int? Unidade_Id { get; set; }

    public int Pessoa_Origem_Categoria_Id { get; set; }

    public decimal? Investimento { get; set; }

    public string? Descricao { get; set; }

    public virtual Pessoa_Origem_Categoria Pessoa_Origem_Categoria { get; set; } = null!;

    public virtual ICollection<Pessoa> Pessoas { get; set; } = new List<Pessoa>();
}
