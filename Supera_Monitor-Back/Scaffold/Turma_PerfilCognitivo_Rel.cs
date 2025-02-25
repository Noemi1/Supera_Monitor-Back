using System;
using System.Collections.Generic;

namespace Supera_Monitor_Back.Scaffold;

public partial class Turma_PerfilCognitivo_Rel
{
    public int Id { get; set; }

    public int PerfilCognitivo_Id { get; set; }

    public int Turma_Id { get; set; }

    public virtual PerfilCognitivo PerfilCognitivo { get; set; } = null!;

    public virtual Turma Turma { get; set; } = null!;
}
