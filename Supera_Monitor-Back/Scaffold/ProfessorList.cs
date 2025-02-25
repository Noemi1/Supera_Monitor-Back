using System;
using System.Collections.Generic;

namespace Supera_Monitor_Back.Scaffold;

public partial class ProfessorList
{
    public int Id { get; set; }

    public int Account_Id { get; set; }

    public string Nome { get; set; } = null!;

    public string Telefone { get; set; } = null!;

    public string Email { get; set; } = null!;

    public DateTime DataInicio { get; set; }

    public string CorLegenda { get; set; } = null!;

    public DateTime? DataNascimento { get; set; }

    public int? Account_Created_Id { get; set; }

    public DateTime Created { get; set; }

    public DateTime? LastUpdated { get; set; }

    public DateTime? Deactivated { get; set; }

    public string Account_Created { get; set; } = null!;
}
