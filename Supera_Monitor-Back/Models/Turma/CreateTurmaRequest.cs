namespace Supera_Monitor_Back.Models.Turma {
    public class CreateTurmaRequest {
        public TimeSpan? Horario { get; set; }
        public int DiaSemana { get; set; }

        public int? Turma_Tipo_Id { get; set; }
        public int? Professor_Id { get; set; }
    }
}
