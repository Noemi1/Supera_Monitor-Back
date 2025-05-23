﻿namespace Supera_Monitor_Back.Entities;

public partial class AspNetUser {
    public string Id { get; set; } = null!;

    public string? Email { get; set; }

    public bool EmailConfirmed { get; set; }

    public string? PasswordHash { get; set; }

    public string? SecurityStamp { get; set; }

    public string? PhoneNumber { get; set; }

    public bool PhoneNumberConfirmed { get; set; }

    public bool TwoFactorEnabled { get; set; }

    public DateTime? LockoutEndDateUtc { get; set; }

    public bool LockoutEnabled { get; set; }

    public int AccessFailedCount { get; set; }

    public string UserName { get; set; } = null!;

    public string? Name { get; set; }

    public bool? Ativo { get; set; }

    public int? Unidade_Id { get; set; }

    public string? EmailSenha { get; set; }

    public int? Cliente_Id { get; set; }
}
