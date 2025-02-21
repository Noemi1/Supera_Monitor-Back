namespace Supera_Monitor_Back.Entities;

public partial class Turma : _BaseEntity {
    public int Id { get; set; }

    public string Nome { get; set; } = null!;

    public int DiaSemana { get; set; }

    public int? Professor_Id { get; set; }

    public TimeSpan? Horario { get; set; }

    public int CapacidadeMaximaAlunos { get; set; }

    public int? Unidade_Id { get; set; }

    public int? Sala_Id { get; set; }

    public virtual ICollection<Aluno> Alunos { get; set; } = new List<Aluno>();

    public virtual ICollection<Aula> Aulas { get; set; } = new List<Aula>();

    public virtual Professor? Professor { get; set; }

    public virtual Sala? Sala { get; set; }

    public virtual ICollection<Turma_PerfilCognitivo_Rel> Turma_PerfilCognitivo_Rel { get; set; } = new List<Turma_PerfilCognitivo_Rel>();
}
