namespace Supera_Monitor_Back.Entities.Views;

public class BaseList {
    public int? Account_Created_Id { get; set; }
    public string? Account_Created { get; set; } = string.Empty;
    public DateTime Created { get; set; }
    public DateTime? LastUpdated { get; set; }
    public DateTime? Deactivated { get; set; }
    public bool Active => !Deactivated.HasValue;
}
