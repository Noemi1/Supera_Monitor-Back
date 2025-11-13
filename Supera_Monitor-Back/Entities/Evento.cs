namespace Supera_Monitor_Back.Entities;

public partial class Evento
{
	
	public int Id { get; set; }
	
	public int Evento_Tipo_Id { get; set; }
	
	public DateTime Data { get; set; }
	
	public int? Sala_Id { get; set; }
	
	public string Descricao { get; set; } = null!;
	
	public string? Observacao { get; set; }
	
	public int DuracaoMinutos { get; set; }
	
	public bool Finalizado { get; set; }
	
	public int Account_Created_Id { get; set; }
	
	public DateTime Created { get; set; }
	
	public DateTime? LastUpdated { get; set; }
	
	public DateTime? Deactivated { get; set; }
	
	public int? ReagendamentoDe_Evento_Id { get; set; }
	
	public int? CapacidadeMaximaAlunos { get; set; }
	
	public virtual Evento_Aula? Evento_Aula { get; set; }
	
	public virtual Evento_Tipo Evento_Tipo { get; set; } = null!;
	
	public virtual Evento? ReagendamentoDe_Evento { get; set; }
	
	public virtual Sala Sala { get; set; } = null!;
	
	public virtual ICollection<Aluno> AlunoAulasZero { get; set; } = new List<Aluno>();
	
	public virtual ICollection<Aluno> AlunoPrimeirasAulas { get; set; } = new List<Aluno>();
	
	public virtual ICollection<Evento_Participacao_Aluno> Evento_Participacao_Aluno { get; set; } = new List<Evento_Participacao_Aluno>();
	
	public virtual ICollection<Evento_Participacao_Aluno> Evento_Participacao_AlunoReposicaoDe_Eventos { get; set; } = new List<Evento_Participacao_Aluno>();
	
	public virtual ICollection<Evento_Participacao_Professor> Evento_Participacao_Professor { get; set; } = new List<Evento_Participacao_Professor>();
	
	public virtual ICollection<Evento> InverseReagendamentoDe_Evento { get; set; } = new List<Evento>();
	
	public virtual ICollection<Aluno_Checklist_Item> Aluno_Checklist_Item { get; set; } = new List<Aluno_Checklist_Item>();
}
