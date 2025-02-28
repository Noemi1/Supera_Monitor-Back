namespace Supera_Monitor_Back.Models.Checklist {
    public class UpdateChecklistItemRequest {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int Ordem { get; set; }
    }
}
