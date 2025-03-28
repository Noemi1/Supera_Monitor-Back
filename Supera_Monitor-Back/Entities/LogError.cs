namespace Supera_Monitor_Back.Entities;

public partial class LogError {
    public int Id { get; set; }

    public string Local { get; set; } = null!;

    public string Message { get; set; } = null!;

    public DateTime? Date { get; set; }
}
