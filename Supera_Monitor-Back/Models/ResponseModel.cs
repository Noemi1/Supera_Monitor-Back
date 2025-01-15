namespace Supera_Monitor_Back.Models {
    public class ResponseModel {
        public dynamic? OldObject { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool Success { get; set; } = false;
        public dynamic? Object { get; set; } = new { };
    }
}
