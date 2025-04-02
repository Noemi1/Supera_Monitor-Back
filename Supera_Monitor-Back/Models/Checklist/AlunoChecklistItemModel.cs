namespace Supera_Monitor_Back.Models.Checklist;

public class AlunoChecklistItemModel {
    public int Id { get; set; }
    public int Aluno_Id { get; set; }
    public DateTime? Prazo { get; set; }
    public string? Observacoes { get; set; } = string.Empty;

    public int Checklist_Item_Id { get; set; }

    public DateTime? DataFinalizacao { get; set; }
    public int? Account_Finalizacao_Id { get; set; }
}
