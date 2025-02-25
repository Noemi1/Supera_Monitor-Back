using System;
using System.Collections.Generic;

namespace Supera_Monitor_Back.Scaffold;

public partial class Professor_AgendaPedagogica
{
    public int Id { get; set; }

    public DateTime Data { get; set; }

    public string Descricao { get; set; } = null!;

    public virtual ICollection<Professor_AgendaPedagogica_Rel> Professor_AgendaPedagogica_Rels { get; set; } = new List<Professor_AgendaPedagogica_Rel>();
}
