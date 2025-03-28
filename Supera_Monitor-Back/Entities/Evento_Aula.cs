namespace Supera_Monitor_Back.Entities;

public partial class Evento_Aula {
    public int Id { get; set; }

    public int Professor_Id { get; set; }

    public int? Turma_Id { get; set; }

    public int CapacidadeMaximaAlunos { get; set; }

    public int? Roteiro_Id { get; set; }

    public virtual ICollection<Evento_Aula_PerfilCognitivo_Rel> Evento_Aula_PerfilCognitivo_Rels { get; set; } = new List<Evento_Aula_PerfilCognitivo_Rel>();

    public virtual Evento Evento { get; set; } = null!;

    public virtual Professor Professor { get; set; } = null!;

    public virtual Roteiro? Roteiro { get; set; }

    public virtual Turma? Turma { get; set; }
}
