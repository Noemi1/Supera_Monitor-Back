using Supera_Monitor_Back.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Supera_Monitor_Back.CRM4U;

[Table("AspNetUsers")]
public partial class AspNetUsersCRM
{
	[Key]
	public string Id { get; set; } = string.Empty;
	public string? Email { get; set; } = string.Empty;
	public bool EmailConfirmed { get; set; }
	public string? PasswordHash { get; set; } = string.Empty;
	public string? SecurityStamp { get; set; } = string.Empty;
	public string? PhoneNumber { get; set; } = string.Empty;
	public bool PhoneNumberConfirmed { get; set; }
	public bool TwoFactorEnabled { get; set; }
	public DateTime? LockoutEndDateUtc { get; set; }
	public bool LockoutEnabled { get; set; }
	public int AccessFailedCount { get; set; }
	public string UserName { get; set; } = string.Empty;
	public string? Name { get; set; } = string.Empty;
	public bool? Ativo { get; set; }
	public int? Unidade_Id { get; set; }
	public string? EmailSenha { get; set; } = string.Empty;
	public int? Cliente_Id { get; set; }
}
