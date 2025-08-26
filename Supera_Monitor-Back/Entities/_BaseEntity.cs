namespace Supera_Monitor_Back.Entities;

public abstract class _BaseEntity {
    public DateTime Created { get; set; }
    public DateTime? LastUpdated { get; set; }
    public DateTime? Deactivated { get; set; }
    public int? Account_Created_Id { get; set; }
    public Account? Account_Created { get; set; } = null!;
    public bool Active => !Deactivated.HasValue;
}
