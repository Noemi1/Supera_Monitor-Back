namespace Supera_Monitor_Back.Entities.Views;

public partial class AlunoChecklistView {
    public int Id { get; set; }

    public int Aluno_Id { get; set; }

    public DateTime? Prazo { get; set; }

    public DateTime? DataFinalizacao { get; set; }

    public int? Account_Finalizacao_Id { get; set; }

    public string? Account_Finalizacao { get; set; }

    public int Checklist_Id { get; set; }

    public int Checklist_Item_Id { get; set; }

    public string Nome { get; set; } = null!;

    public int Ordem { get; set; }
}
