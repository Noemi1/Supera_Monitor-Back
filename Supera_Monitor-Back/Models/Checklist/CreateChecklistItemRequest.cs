namespace Supera_Monitor_Back.Models.Checklist {
    public class CreateChecklistItemRequest {
        public int Checklist_Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int Ordem { get; set; }
    }
}
