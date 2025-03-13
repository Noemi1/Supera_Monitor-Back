using Supera_Monitor_Back.Entities.Views;

namespace Supera_Monitor_Back.Models.Checklist;

public class ChecklistsFromAlunoModel {
    public int Aluno_Id { get; set; }
    public List<AlunoChecklistView> Checklist { get; set; } = new List<AlunoChecklistView>();
}
