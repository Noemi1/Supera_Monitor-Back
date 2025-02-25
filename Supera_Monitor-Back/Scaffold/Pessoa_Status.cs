using System;
using System.Collections.Generic;

namespace Supera_Monitor_Back.Scaffold;

public partial class Pessoa_Status
{
    public int Id { get; set; }

    public string Nome { get; set; } = null!;

    public virtual ICollection<Pessoa> Pessoas { get; set; } = new List<Pessoa>();
}
