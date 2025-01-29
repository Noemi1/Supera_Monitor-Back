namespace Supera_Monitor_Back.Models.Aula {
    public class CreateAulaRequest {
        public int Turma_Id { get; set; }
        public DateTime Data { get; set; }
        public int Professor_Id { get; set; }
    }
}
