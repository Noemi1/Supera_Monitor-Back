using System;
using System.Collections.Generic;

namespace Supera_Monitor_Back.Scaffold;

public partial class Professor_NivelCertificacao
{
    public int Id { get; set; }

    public string Descricao { get; set; } = null!;

    public virtual ICollection<Professor> Professors { get; set; } = new List<Professor>();
}
