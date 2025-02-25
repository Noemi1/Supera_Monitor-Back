using System;
using System.Collections.Generic;

namespace Supera_Monitor_Back.Scaffold;

public partial class Professor
{
    public int Id { get; set; }

    public int Account_Id { get; set; }

    public int? Professor_NivelCertificacao_Id { get; set; }

    public DateTime DataInicio { get; set; }

    public string CorLegenda { get; set; } = null!;

    public DateTime? DataNascimento { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual ICollection<Aula> Aulas { get; set; } = new List<Aula>();

    public virtual ICollection<Professor_AgendaPedagogica_Rel> Professor_AgendaPedagogica_Rels { get; set; } = new List<Professor_AgendaPedagogica_Rel>();

    public virtual Professor_NivelCertificacao? Professor_NivelCertificacao { get; set; }

    public virtual ICollection<Turma> Turmas { get; set; } = new List<Turma>();
}
