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
	
	AgendamentoPrimeiraAula = 38,
	ComparecimentoPrimeiraAula = 3,

	Agendamento1Oficina = 12,
	Comparecimento1Oficina = 34,
	
	Agendamento2Oficina = 23,
	Comparecimento2Oficina = 36,

	Agendamento1Superacao = 22,
	Comparecimento1Superacao = 35,

	Agendamento2Superacao = 29,
	Comparecimento2Superacao = 40,

}