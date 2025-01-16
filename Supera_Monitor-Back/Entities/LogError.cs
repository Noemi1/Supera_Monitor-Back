namespace Supera_Monitor_Back.Entities {
    public partial class LogError {
        public int Id { get; set; }
        public string Local { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime? Date { get; set; }
    }
}
