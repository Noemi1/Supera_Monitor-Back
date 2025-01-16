namespace Supera_Monitor_Back.Entities {
    public class Log {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Object { get; set; } = string.Empty;
        public string Entity { get; set; } = string.Empty;
        public int? Account_Id { get; set; }

        public virtual Account? Account { get; set; }
    }
}
