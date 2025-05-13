namespace Supera_Monitor_Back.Entities.Views;

public partial class AlunoChecklistItemList {
    public int Id { get; set; }

    public string Checklist_Item { get; set; } = null!;

    public int Checklist_Item_Id { get; set; }

    public DateTime? Prazo { get; set; }

    public string? Observacoes { get; set; }

    public int Checklist_Id { get; set; }

    public string Checklist { get; set; } = null!;

    public int Aluno_Id { get; set; }

    public string? Aluno { get; set; }

    public string? Celular { get; set; }

    public int Finalizado { get; set; }

    public int? Account_Finalizacao_Id { get; set; }

    public DateTime? DataFinalizacao { get; set; }

    public string? Account_Finalizacao { get; set; }

    public int Turma_Id { get; set; }

    public string Turma { get; set; } = null!;

    public int? Professor_Id { get; set; }

    public string? Professor { get; set; }

    public string CorLegenda { get; set; } = null!;
}
