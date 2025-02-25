using System;
using System.Collections.Generic;

namespace Supera_Monitor_Back.Scaffold;

public partial class TurmaList
{
    public int Id { get; set; }

    public string Nome { get; set; } = null!;

    public int DiaSemana { get; set; }

    public TimeSpan? Horario { get; set; }

    public int CapacidadeMaximaAlunos { get; set; }

    public int? Unidade_Id { get; set; }

    public int Account_Created_Id { get; set; }

    public string Account_Created { get; set; } = null!;

    public DateTime Created { get; set; }

    public DateTime? LastUpdated { get; set; }

    public DateTime? Deactivated { get; set; }

    public int? Professor_Id { get; set; }

    public string? Professor { get; set; }

    public string? CorLegenda { get; set; }
}
