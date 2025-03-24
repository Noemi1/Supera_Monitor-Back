namespace Supera_Monitor_Back.Models.Roteiro;

public class MaterialModel {
    public int Id { get; set; }

    public string FileName { get; set; } = null!;

    public string FileBase64 { get; set; } = null!;

    public int Roteiro_Id { get; set; }

    public int Account_Created_Id { get; set; }

    public DateTime Created { get; set; }

    public DateTime? LastUpdated { get; set; }

    public DateTime? Deactivated { get; set; }
}
