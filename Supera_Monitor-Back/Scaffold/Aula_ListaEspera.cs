using System;
using System.Collections.Generic;

namespace Supera_Monitor_Back.Scaffold;

public partial class Aula_ListaEspera
{
    public int Id { get; set; }

    public int Aula_Id { get; set; }

    public int Aluno_Id { get; set; }

    public int Account_Created_Id { get; set; }

    public DateTime Created { get; set; }

    public virtual Account Account_Created { get; set; } = null!;

    public virtual Aluno Aluno { get; set; } = null!;

    public virtual Aula Aula { get; set; } = null!;
}
