namespace Supera_Monitor_Back.Models.Professor {
    public class UpdateProfessorRequest {
        public int Id { get; set; }

        public int NivelAH { get; set; }
        public int NivelAbaco { get; set; }
        public DateTime DataInicio { get; set; }
    }
}
