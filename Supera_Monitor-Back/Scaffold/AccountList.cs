using System;
using System.Collections.Generic;

namespace Supera_Monitor_Back.Scaffold;

public partial class AccountList
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public DateTime? Verified { get; set; }

    public DateTime? PasswordReset { get; set; }

    public int Role_Id { get; set; }

    public int? Account_Created_Id { get; set; }

    public DateTime Created { get; set; }

    public DateTime? LastUpdated { get; set; }

    public DateTime? Deactivated { get; set; }

    public string? Role { get; set; }

    public string? Account_Created { get; set; }
}
