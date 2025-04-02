namespace Supera_Monitor_Back.Models.Checklist;

public class ToggleAlunoChecklistRequest {
    public int Aluno_Checklist_Item_Id { get; set; }
    public string? Observacoes { get; set; } = string.Empty;
}
