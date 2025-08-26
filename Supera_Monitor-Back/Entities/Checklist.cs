namespace Supera_Monitor_Back.Entities;

public partial class Checklist {
    public int Id { get; set; }
    public string Nome { get; set; } = null!;
    public int Ordem { get; set; }
    public int? NumeroSemana { get; set; }
    public virtual ICollection<Checklist_Item> Checklist_Items { get; set; } = new List<Checklist_Item>();
}
