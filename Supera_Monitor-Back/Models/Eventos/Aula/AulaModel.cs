namespace Supera_Monitor_Back.Models.Eventos.Aula;

public class AulaModel {
    public int? Turma_Id { get; set; }

    public string? Turma { get; set; }

    public int CapacidadeMaximaAlunos { get; set; }

    public int Professor_Id { get; set; }

    public string Professor { get; set; } = null!;

    public int? Roteiro_Id { get; set; }

    public string? Roteiro { get; set; }

    public virtual ICollection<PerfilCognitivoModel> PerfilCognitivo { get; set; } = new HashSet<PerfilCognitivoModel>();

}
