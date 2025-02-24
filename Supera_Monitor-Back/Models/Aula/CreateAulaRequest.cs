namespace Supera_Monitor_Back.Models.Aula {
    public class CreateAulaRequest {
        public DateTime Data { get; set; }

        public int Sala_Id { get; set; }
        public int Professor_Id { get; set; }

        public int? Turma_Id { get; set; }
        public string? Observacao { get; set; }
    }
}
