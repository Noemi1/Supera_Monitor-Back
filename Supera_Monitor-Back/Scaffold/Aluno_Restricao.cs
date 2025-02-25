using System;
using System.Collections.Generic;

namespace Supera_Monitor_Back.Scaffold;

public partial class Aluno_Restricao
{
    public int Id { get; set; }

    public string Restricao { get; set; } = null!;

    public virtual ICollection<Aluno_Restricao_Rel> Aluno_Restricao_Rels { get; set; } = new List<Aluno_Restricao_Rel>();
}
