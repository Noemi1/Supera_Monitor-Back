using Supera_Monitor_Back.Entities;

namespace Supera_Monitor_Back.Services.Email.Models;

public class ProfessorReposicaoEmailModel {
    public string Name { get; set; } = string.Empty;

    public string AlunoName { get; set; } = string.Empty;
    public string TurmaName { get; set; } = string.Empty;
    public DateTime OldDate { get; set; }
    public DateTime NewDate { get; set; }

    public ICollection<Pessoa> Pessoas { get; set; } = new List<Pessoa>();
}
