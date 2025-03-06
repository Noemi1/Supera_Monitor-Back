namespace Supera_Monitor_Back.Models.Aula {
    public class UpdateAulaRequest {
        public int Id { get; set; }

        public int Sala_Id { get; set; }
        public int Professor_Id { get; set; }

        public string? Observacao { get; set; }
        public string? Descricao { get; set; }
    }
}
