namespace Supera_Monitor_Back.Entities;

public partial class PerfilCognitivo
{
	public int Id { get; set; }

	public string Nome { get; set; } = null!;

	public string? Descricao { get; set; }

	public virtual ICollection<Evento_Aula_PerfilCognitivo_Rel> Evento_Aula_PerfilCognitivo_Rels { get; set; } = new List<Evento_Aula_PerfilCognitivo_Rel>();

	public virtual ICollection<Turma_PerfilCognitivo_Rel> Turma_PerfilCognitivo_Rels { get; set; } = new List<Turma_PerfilCognitivo_Rel>();
}
