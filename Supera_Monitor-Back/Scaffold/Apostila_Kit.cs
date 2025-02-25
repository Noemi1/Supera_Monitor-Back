using System;
using System.Collections.Generic;

namespace Supera_Monitor_Back.Scaffold;

public partial class Apostila_Kit
{
    public int Id { get; set; }

    public string Nome { get; set; } = null!;

    public virtual ICollection<Aluno> Alunos { get; set; } = new List<Aluno>();

    public virtual ICollection<Apostila_Kit_Rel> Apostila_Kit_Rels { get; set; } = new List<Apostila_Kit_Rel>();
}
