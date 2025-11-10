namespace Supera_Monitor_Back.Entities;

public partial class AspNetUser
{
	public string Id { get; set; } = null!;
	public string? Name { get; set; }
	public string UserName { get; set; } = null!;
	public string? Email { get; set; }
	public string? PhoneNumber { get; set; }
	public virtual ICollection<Aluno_Historico> Aluno_Historicos { get; set; } = new List<Aluno_Historico>();
}
