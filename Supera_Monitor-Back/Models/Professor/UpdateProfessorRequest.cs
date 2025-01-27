namespace Supera_Monitor_Back.Models.Professor {
    public class UpdateProfessorRequest {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
