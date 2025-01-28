namespace Supera_Monitor_Back.Models.Professor {
    public class CreateProfessorRequest {
        public int Account_Id { get; set; }

        public int NivelAH { get; set; }
        public int NivelAbaco { get; set; }
        public DateTime DataInicio { get; set; }
    }
}
