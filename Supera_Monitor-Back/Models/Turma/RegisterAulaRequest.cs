namespace Supera_Monitor_Back.Models.Turma {
    public class RegisterAulaRequest {
        public int Turma_Id { get; set; }
        public DateTime Data { get; set; }
        public int Professor_Id { get; set; }
    }
}
