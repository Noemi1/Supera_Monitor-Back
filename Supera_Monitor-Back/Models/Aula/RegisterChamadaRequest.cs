namespace Supera_Monitor_Back.Models.Aula {
    public class RegisterChamadaRequest {
        public int Aula_Id { get; set; }
        public int Professor_Id { get; set; }

        public List<UpdateRegistroRequest> Registros { get; set; } = new();
    }
}
