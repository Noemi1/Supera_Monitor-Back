using System;
using System.Collections.Generic;

namespace Supera_Monitor_Back.Scaffold;

public partial class LogList
{
    public int Id { get; set; }

    public DateTime Date { get; set; }

    public string Action { get; set; } = null!;

    public string Entity { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public int? Account_Id { get; set; }
}
