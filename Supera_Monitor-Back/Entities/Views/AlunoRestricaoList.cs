namespace Supera_Monitor_Back.Entities.Views;

public partial class AlunoRestricaoList {
    public int Id { get; set; }

    public string Descricao { get; set; } = null!;

    public int Aluno_Id { get; set; }

    public int Account_Created_Id { get; set; }

    public DateTime Created { get; set; }

    public DateTime? Deactivated { get; set; }
}