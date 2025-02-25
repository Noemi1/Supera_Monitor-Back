using System;
using System.Collections.Generic;

namespace Supera_Monitor_Back.Scaffold;

public partial class AccountRole
{
    public int Id { get; set; }

    public string? Role { get; set; }

    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();
}
