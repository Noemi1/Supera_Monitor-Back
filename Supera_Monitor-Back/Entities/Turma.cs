namespace Supera_Monitor_Back.Entities;

public partial class Turma
{
	public int Id { get; set; }
	public string Nome { get; set; } = null!;
	public int DiaSemana { get; set; }
	public int? Professor_Id { get; set; }
	public TimeSpan? Horario { get; set; }
	public int CapacidadeMaximaAlunos { get; set; }
	public int? Unidade_Id { get; set; }
	public DateTime Created { get; set; }
	public DateTime? LastUpdated { get; set; }
	public DateTime? Deactivated { get; set; }
	public int Account_Created_Id { get; set; }
	public int? Sala_Id { get; set; }
	public string? LinkGrupo { get; set; }
	public bool Active => !Deactivated.HasValue;
	public virtual Account Account_Created { get; set; } = null!;
	public virtual Professor? Professor { get; set; }
	public virtual Sala? Sala { get; set; }

	public virtual ICollection<Aluno> Alunos { get; set; } = new List<Aluno>();
	public virtual ICollection<Evento_Aula> Evento_Aulas { get; set; } = new List<Evento_Aula>();
	public virtual ICollection<Aluno_Turma_Vigencia> Aluno_Turma_Vigencia { get; set; } = new List<Aluno_Turma_Vigencia>();
	public virtual ICollection<Turma_PerfilCognitivo_Rel> Turma_PerfilCognitivo_Rels { get; set; } = new List<Turma_PerfilCognitivo_Rel>();

	public bool PossuiVagas(int vagasOcupadas, int vagasRequisitadas)
	{
		int vagasDisponiveis = this.CapacidadeMaximaAlunos - vagasOcupadas;

		return (vagasDisponiveis - vagasRequisitadas) >= 0;
	}

}
