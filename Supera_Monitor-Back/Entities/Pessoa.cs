namespace Supera_Monitor_Back.Entities;

public partial class Pessoa {
    public int Id { get; set; }

    public string? Nome { get; set; }

    public DateTime? DataNascimento { get; set; }

    public virtual ICollection<Aluno> Alunos { get; set; } = new List<Aluno>();
}
