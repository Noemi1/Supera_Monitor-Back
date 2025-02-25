using System;
using System.Collections.Generic;

namespace Supera_Monitor_Back.Scaffold;

public partial class CalendarioList
{
    public int Aula_Id { get; set; }

    public DateTime Data { get; set; }

    public int? Turma_Id { get; set; }

    public string? Turma { get; set; }

    public int? CapacidadeMaximaAlunos { get; set; }

    public bool? Finalizada { get; set; }

    public int Professor_Id { get; set; }

    public string Professor { get; set; } = null!;

    public string CorLegenda { get; set; } = null!;

    public string? Observacao { get; set; }

    public DateTime? Deactivated { get; set; }

    public int? ReposicaoDe_Aula_Id { get; set; }

    public int Sala_Id { get; set; }
}
