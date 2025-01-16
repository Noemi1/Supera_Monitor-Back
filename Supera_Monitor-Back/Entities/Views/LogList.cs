namespace Supera_Monitor_Back.Entities.Views {
    public class LogList {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Entity { get; set; } = string.Empty;

        // Account
        public int? Account_Id { get; set; }
        public string? AccountName { get; set; }
        public string? AccountEmail { get; set; }
    }
}
