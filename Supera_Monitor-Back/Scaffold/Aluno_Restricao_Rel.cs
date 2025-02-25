using System;
using System.Collections.Generic;

namespace Supera_Monitor_Back.Scaffold;

public partial class Aluno_Restricao_Rel
{
    public int Id { get; set; }

    public int Aluno_Id { get; set; }

    public int Restricao_Id { get; set; }

    public virtual Aluno Aluno { get; set; } = null!;

    public virtual Aluno_Restricao Restricao { get; set; } = null!;
}
