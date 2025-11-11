namespace Supera_Monitor_Back.Entities;

public partial class Aluno_Checklist_Item
{
	public int Id { get; set; }
	public int Aluno_Id { get; set; }
	public int Checklist_Item_Id { get; set; }
	public int? Evento_Id { get; set; }
	public DateTime Prazo { get; set; }
	public DateTime? DataFinalizacao { get; set; }
	public int? Account_Finalizacao_Id { get; set; }
	public string? Observacoes { get; set; }
	public virtual Account? Account_Finalizacao { get; set; }
	public virtual Aluno Aluno { get; set; } = null!;
	public virtual Checklist_Item Checklist_Item { get; set; } = null!;
	public virtual Evento? Evento { get; set; }
}
