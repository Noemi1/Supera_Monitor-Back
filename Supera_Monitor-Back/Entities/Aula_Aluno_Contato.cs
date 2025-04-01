namespace Supera_Monitor_Back.Entities;

public partial class Aula_Aluno_Contato {
    public int Id { get; set; }

    public int Aula_Aluno_Id { get; set; }

    public int Account_Id { get; set; }

    public DateTime Data { get; set; }

    public string Observacoes { get; set; } = null!;

    public virtual Account Account { get; set; } = null!;
}
