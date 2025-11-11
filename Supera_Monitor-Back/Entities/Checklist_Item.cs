namespace Supera_Monitor_Back.Entities;

public partial class Checklist_Item
{
	public int Id { get; set; }
	public string Nome { get; set; } = null!;
	public int Ordem { get; set; }
	public int Checklist_Id { get; set; }
	public DateTime? Deactivated { get; set; }
	public virtual ICollection<Aluno_Checklist_Item> Aluno_Checklist_Items { get; set; } = new List<Aluno_Checklist_Item>();
	public virtual Checklist Checklist { get; set; } = null!;
}


public enum ChecklistItemId
{
	AgendamentoAulaZero = 31,
	ComparecimentoAulaZero = 33,
}