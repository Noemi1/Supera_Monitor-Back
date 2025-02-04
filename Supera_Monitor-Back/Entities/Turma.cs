namespace Supera_Monitor_Back.Entities;

public partial class Turma : _BaseEntity {
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public int DiaSemana { get; set; }

    public TimeSpan? Horario { get; set; }

    public int CapacidadeMaximaAlunos { get; set; }

    public int? Unidade_Id { get; set; }

    public int? Professor_Id { get; set; }
    public virtual Professor? Professor { get; set; }

    public int? Turma_Tipo_Id { get; set; }
    public virtual TurmaTipo? Turma_Tipo { get; set; }

    public virtual ICollection<Aluno> Alunos { get; set; } = new HashSet<Aluno>();
    public virtual ICollection<TurmaAula> Turma_Aulas { get; set; } = new HashSet<TurmaAula>();
}
