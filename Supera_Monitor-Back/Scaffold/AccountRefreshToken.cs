using System;
using System.Collections.Generic;

namespace Supera_Monitor_Back.Scaffold;

public partial class AccountRefreshToken
{
    public int Id { get; set; }

    public int Account_Id { get; set; }

    public string Token { get; set; } = null!;

    public DateTime Expires { get; set; }

    public DateTime Created { get; set; }

    public string? CreatedByIp { get; set; }

    public DateTime? Revoked { get; set; }

    public string? RevokedByIp { get; set; }

    public string? ReplacedByToken { get; set; }

    public virtual Account Account { get; set; } = null!;
}
