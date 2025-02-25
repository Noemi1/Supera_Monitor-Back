using System;
using System.Collections.Generic;

namespace Supera_Monitor_Back.Scaffold;

public partial class ApostilaList
{
    public int Id { get; set; }

    public string Nome { get; set; } = null!;

    public int NumeroTotalPaginas { get; set; }

    public int Ordem { get; set; }

    public int Apostila_Tipo_Id { get; set; }

    public int Apostila_Kit_Id { get; set; }

    public string Kit { get; set; } = null!;
}
