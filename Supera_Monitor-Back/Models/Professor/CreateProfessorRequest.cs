namespace Supera_Monitor_Back.Models.Professor {
    public class CreateProfessorRequest {
        public int? Account_Id { get; set; }

        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        public string CorLegenda { get; set; } = string.Empty;
        public DateTime DataInicio { get; set; }

        public int Professor_NivelCertificacao_Id { get; set; }
    }
}
