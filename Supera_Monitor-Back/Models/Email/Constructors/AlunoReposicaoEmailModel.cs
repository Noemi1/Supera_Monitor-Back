namespace Supera_Monitor_Back.Models.Email.Constructors;

public class AlunoReposicaoEmailModel {
    public string Name { get; set; } = string.Empty;
    public DateTime OldDate { get; set; }
    public DateTime NewDate { get; set; }
    public string TurmaName { get; set; } = string.Empty;
}
