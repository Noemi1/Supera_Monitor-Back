using System;
using System.Collections.Generic;

namespace Supera_Monitor_Back.Scaffold;

public partial class Apostila_Kit_Rel
{
    public int Id { get; set; }

    public int Apostila_Id { get; set; }

    public int Apostila_Kit_Id { get; set; }

    public virtual Apostila Apostila { get; set; } = null!;

    public virtual Apostila_Kit Apostila_Kit { get; set; } = null!;
}
