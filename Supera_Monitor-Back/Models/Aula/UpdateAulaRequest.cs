namespace Supera_Monitor_Back.Models.Aula {
    public class UpdateAulaRequest {
        public int Id { get; set; }

        public DateTime Data { get; set; }
        public int Professor_Id { get; set; }
    }
}
