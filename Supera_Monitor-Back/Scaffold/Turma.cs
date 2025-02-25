using System;
using System.Collections.Generic;

namespace Supera_Monitor_Back.Scaffold;

public partial class Turma
{
    public int Id { get; set; }

    public string Nome { get; set; } = null!;

    public int DiaSemana { get; set; }

    public int? Professor_Id { get; set; }

    public TimeSpan? Horario { get; set; }

    public int CapacidadeMaximaAlunos { get; set; }

    public int? Unidade_Id { get; set; }

    public DateTime Created { get; set; }

    public DateTime? LastUpdated { get; set; }

    public DateTime? Deactivated { get; set; }

    public int Account_Created_Id { get; set; }

    public int? Sala_Id { get; set; }

    public virtual Account Account_Created { get; set; } = null!;

    public virtual ICollection<Aluno> Alunos { get; set; } = new List<Aluno>();

    public virtual ICollection<Aula> Aulas { get; set; } = new List<Aula>();

    public virtual Professor? Professor { get; set; }

    public virtual Sala? Sala { get; set; }

    public virtual ICollection<Turma_PerfilCognitivo_Rel> Turma_PerfilCognitivo_Rels { get; set; } = new List<Turma_PerfilCognitivo_Rel>();
}
