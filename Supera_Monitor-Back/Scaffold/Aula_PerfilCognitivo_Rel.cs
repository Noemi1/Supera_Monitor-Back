using System;
using System.Collections.Generic;

namespace Supera_Monitor_Back.Scaffold;

public partial class Aula_PerfilCognitivo_Rel
{
    public int Id { get; set; }

    public int Aula_Id { get; set; }

    public int PerfilCognitivo_Id { get; set; }

    public virtual Aula Aula { get; set; } = null!;

    public virtual PerfilCognitivo PerfilCognitivo { get; set; } = null!;
}
