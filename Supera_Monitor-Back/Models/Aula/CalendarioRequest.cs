namespace Supera_Monitor_Back.Models.Aula {
    public class CalendarioRequest {
        public DateTime? IntervaloDe { get; set; }
        public DateTime? IntervaloAte { get; set; }
        public int? Turma_Id { get; set; }
        public int? Professor_Id { get; set; }
        public int? Aluno_Id { get; set; }

        public int? Perfil_Cognitivo_Id { get; set; }
    }
}
