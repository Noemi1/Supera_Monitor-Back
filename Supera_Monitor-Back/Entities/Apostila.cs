namespace Supera_Monitor_Back.Entities;

public partial class Apostila {
    public int Id { get; set; }

    public string Nome { get; set; } = null!;

    public int NumeroTotalPaginas { get; set; }

    public int Ordem { get; set; }

    public int Apostila_Tipo_Id { get; set; }

    public virtual ICollection<Apostila_Kit_Rel> Apostila_Kit_Rels { get; set; } = new List<Apostila_Kit_Rel>();

    public virtual ICollection<Aula_Aluno> Aula_AlunoApostila_AHs { get; set; } = new List<Aula_Aluno>();

    public virtual ICollection<Aula_Aluno> Aula_AlunoApostila_Abacos { get; set; } = new List<Aula_Aluno>();
}
