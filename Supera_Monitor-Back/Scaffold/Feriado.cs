using System;
using System.Collections.Generic;

namespace Supera_Monitor_Back.Scaffold;

public partial class Feriado
{
    public int Id { get; set; }

    public DateTime Data { get; set; }

    public string Descricao { get; set; } = null!;
}
