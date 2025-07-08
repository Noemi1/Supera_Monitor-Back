namespace Supera_Monitor_Back.Entities;

public partial class Aluno_Historico {
    public int Id { get; set; }

    public int? Account_Id { get; set; }

    public string? AspNetUser_Id { get; set; }

    public int Aluno_Id { get; set; }

    public string Descricao { get; set; } = null!;

    public DateTime Data { get; set; }

    public virtual Account? Account { get; set; } = null!;

    public virtual AspNetUser? AspNetUser { get; set; } = null!;

    public virtual Aluno Aluno { get; set; } = null!;
}
