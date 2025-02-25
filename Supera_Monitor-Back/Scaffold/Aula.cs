using System;
using System.Collections.Generic;

namespace Supera_Monitor_Back.Scaffold;

public partial class Aula
{
    public int Id { get; set; }

    public int? Turma_Id { get; set; }

    public DateTime Data { get; set; }

    public int Professor_Id { get; set; }

    public string? Observacao { get; set; }

    public bool? Finalizada { get; set; }

    public int Sala_Id { get; set; }

    public int? ReposicaoDe_Aula_Id { get; set; }

    public int? Account_Created_Id { get; set; }

    public DateTime Created { get; set; }

    public DateTime? Deactivated { get; set; }

    public DateTime? LastUpdated { get; set; }

    public virtual Account? Account_Created { get; set; }

    public virtual ICollection<Aula_Aluno> Aula_Alunos { get; set; } = new List<Aula_Aluno>();

    public virtual ICollection<Aula_ListaEspera> Aula_ListaEsperas { get; set; } = new List<Aula_ListaEspera>();

    public virtual ICollection<Aula_PerfilCognitivo_Rel> Aula_PerfilCognitivo_Rels { get; set; } = new List<Aula_PerfilCognitivo_Rel>();

    public virtual ICollection<Aula> InverseReposicaoDe_Aula { get; set; } = new List<Aula>();

    public virtual Professor Professor { get; set; } = null!;

    public virtual Aula? ReposicaoDe_Aula { get; set; }

    public virtual Sala Sala { get; set; } = null!;

    public virtual Turma? Turma { get; set; }
}
