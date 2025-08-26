namespace Supera_Monitor_Back.Entities;

public partial class Aluno_Restricao {
    public int Id { get; set; }
    public string Descricao { get; set; } = null!;
    public int Aluno_Id { get; set; }
    public int Account_Created_Id { get; set; }
    public DateTime Created { get; set; }
    public DateTime? Deactivated { get; set; }
    public virtual Account Account_Created { get; set; } = null!;
    public virtual Aluno Aluno { get; set; } = null!;
}
