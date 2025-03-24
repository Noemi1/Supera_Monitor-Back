namespace Supera_Monitor_Back.Models.Roteiro;

public class CreateRoteiroMaterialRequest {
    public int Roteiro_Id { get; set; }

    public string FileName { get; set; } = null!;

    public string FileBase64 { get; set; } = null!;
}
