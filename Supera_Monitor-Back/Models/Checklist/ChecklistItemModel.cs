namespace Supera_Monitor_Back.Models.Checklist {
    public class ChecklistItemModel {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int Ordem { get; set; }
        public DateTime? Deactivated { get; set; }
        public int Checklist_Id { get; set; }
    }
}
