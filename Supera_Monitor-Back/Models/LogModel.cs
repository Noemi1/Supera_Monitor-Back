namespace Supera_Monitor_Back.Models {
    public class LogModel {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Object { get; set; } = string.Empty;
        public string Entity { get; set; } = string.Empty;

        public int? Account_Id { get; set; }
        public string? AccountName { get; set; }
        public string? AccountEmail { get; set; }
    }
}
