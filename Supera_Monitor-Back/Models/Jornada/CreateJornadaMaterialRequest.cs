namespace Supera_Monitor_Back.Models.Jornada;

public class CreateJornadaMaterialRequest {
    public int Jornada_Id { get; set; }

    public string FileName { get; set; } = null!;

    public string FileBase64 { get; set; } = null!;
}
