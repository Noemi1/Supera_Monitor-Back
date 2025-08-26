namespace Supera_Monitor_Back.Models.Checklist;

public class ChecklistModel {
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int Ordem { get; set; }
    public int NumeroSemana { get; set; }
    public virtual ICollection<ChecklistItemModel> Items { get; set; } = new List<ChecklistItemModel>();
}
