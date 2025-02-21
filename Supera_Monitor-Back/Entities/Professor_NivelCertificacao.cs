namespace Supera_Monitor_Back.Entities;

public partial class Professor_NivelCertificacao {
    public int Id { get; set; }

    public string Descricao { get; set; } = null!;

    public virtual ICollection<Professor> Professor { get; set; } = new List<Professor>();
}
