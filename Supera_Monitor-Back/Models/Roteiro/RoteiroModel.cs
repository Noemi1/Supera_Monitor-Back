namespace Supera_Monitor_Back.Models.Roteiro;

public class RoteiroModel {
    public int Id { get; set; }

    public string Tema { get; set; } = null!;

    public int Semana { get; set; }

    public DateTime DataInicio { get; set; }

    public DateTime DataFim { get; set; }

	public string CorLegenda { get; set; } = String.Empty;

	public bool Recesso { get; set; }

	public int Account_Created_Id { get; set; }

    public DateTime Created { get; set; }

    public DateTime? LastUpdated { get; set; }

    public DateTime? Deactivated { get; set; }
}
