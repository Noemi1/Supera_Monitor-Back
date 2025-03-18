namespace Supera_Monitor_Back.Services.Email.Models;

public class ReagendarAulaModel {
    public string Name { get; set; } = string.Empty;
    public string TurmaName { get; set; } = string.Empty;
    public DateTime OldDate { get; set; }
    public DateTime NewDate { get; set; }
}
