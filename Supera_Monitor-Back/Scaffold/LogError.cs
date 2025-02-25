using System;
using System.Collections.Generic;

namespace Supera_Monitor_Back.Scaffold;

public partial class LogError
{
    public int Id { get; set; }

    public string Local { get; set; } = null!;

    public string Message { get; set; } = null!;

    public DateTime? Date { get; set; }
}
