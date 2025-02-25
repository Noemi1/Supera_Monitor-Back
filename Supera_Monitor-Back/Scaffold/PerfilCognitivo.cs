using System;
using System.Collections.Generic;

namespace Supera_Monitor_Back.Scaffold;

public partial class PerfilCognitivo
{
    public int Id { get; set; }

    public string Nome { get; set; } = null!;

    public virtual ICollection<Aula_PerfilCognitivo_Rel> Aula_PerfilCognitivo_Rels { get; set; } = new List<Aula_PerfilCognitivo_Rel>();

    public virtual ICollection<Turma_PerfilCognitivo_Rel> Turma_PerfilCognitivo_Rels { get; set; } = new List<Turma_PerfilCognitivo_Rel>();
}
