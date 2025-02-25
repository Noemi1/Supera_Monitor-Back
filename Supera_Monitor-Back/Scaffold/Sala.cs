using System;
using System.Collections.Generic;

namespace Supera_Monitor_Back.Scaffold;

public partial class Sala
{
    public int Id { get; set; }

    public int NumeroSala { get; set; }

    public int Andar { get; set; }

    public virtual ICollection<Aula> Aulas { get; set; } = new List<Aula>();

    public virtual ICollection<Turma> Turmas { get; set; } = new List<Turma>();
}
