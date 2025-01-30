namespace Supera_Monitor_Back.Models.Professor {
    public class CreateProfessorRequest {
        public int? Account_Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        public int NivelAH { get; set; }
        public int NivelAbaco { get; set; }
        public DateTime DataInicio { get; set; }
    }
}
