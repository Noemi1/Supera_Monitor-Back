namespace Supera_Monitor_Back.Models.Professor {
    public class CreateProfessorRequest {
        public int? Account_Id { get; set; }

        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;

        public int Professor_NivelAH_Id { get; set; }
        public int Professor_NivelAbaco_Id { get; set; }
        public DateTime DataInicio { get; set; }
    }
}
