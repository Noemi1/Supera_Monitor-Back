using System;
using System.Collections.Generic;

namespace Supera_Monitor_Back.Scaffold;

public partial class Aula_Aluno
{
    public int Id { get; set; }

    public int Aula_Id { get; set; }

    public int Aluno_Id { get; set; }

    public bool? Presente { get; set; }

    public int? NumeroPaginaAbaco { get; set; }

    public int? NumeroPaginaAH { get; set; }

    public int? Apostila_Abaco_Id { get; set; }

    public int? Apostila_AH_Id { get; set; }

    public string? Observacao { get; set; }

    public int? ReposicaoDe_Aula_Id { get; set; }

    public virtual Aluno Aluno { get; set; } = null!;

    public virtual Apostila? Apostila_AH { get; set; }

    public virtual Apostila? Apostila_Abaco { get; set; }

    public virtual Aula? ReposicaoDe_Aula { get; set; }
}
