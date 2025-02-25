using System;
using System.Collections.Generic;

namespace Supera_Monitor_Back.Scaffold;

public partial class Jornadum
{
    public int Id { get; set; }

    public string Tema { get; set; } = null!;

    public int Semana { get; set; }

    public DateTime DataInicio { get; set; }

    public DateTime DataFim { get; set; }
}
