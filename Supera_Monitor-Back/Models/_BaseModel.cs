namespace Supera_Monitor_Back.Models;

public class _BaseModel {
    public int Account_Created_Id { get; set; }
    public string Account_Created_Name { get; set; } = string.Empty;
    public DateTime Created { get; set; }
    public DateTime? LastUpdated { get; set; }
    public DateTime? Deactivated { get; set; }
    public bool Active => !Deactivated.HasValue;
}
