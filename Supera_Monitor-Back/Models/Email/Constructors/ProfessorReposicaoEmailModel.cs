namespace Supera_Monitor_Back.Models.Email.Constructors;

public class ProfessorReposicaoEmailModel {
    public string Name { get; set; } = string.Empty;
    public string AlunoName { get; set; } = string.Empty;
    public string TurmaName { get; set; } = string.Empty;
    public DateTime OldDate { get; set; }
    public DateTime NewDate { get; set; }
    public ICollection<Entities.Pessoa> Pessoas { get; set; } = new List<Entities.Pessoa>();
}
