namespace Supera_Monitor_Back.Models;

public class ResponseModel {
    public bool Success { get; set; } = false;
    public string Message { get; set; } = string.Empty;
    public dynamic? Object { get; set; } = new { };
    public dynamic? OldObject { get; set; }
}
