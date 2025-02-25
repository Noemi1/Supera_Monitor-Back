using System;
using System.Collections.Generic;

namespace Supera_Monitor_Back.Scaffold;

public partial class Pessoa_Origem_Categorium
{
    public int Id { get; set; }

    public string Nome { get; set; } = null!;

    public virtual ICollection<Pessoa_Origem> Pessoa_Origems { get; set; } = new List<Pessoa_Origem>();
}
