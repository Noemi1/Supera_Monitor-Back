namespace Supera_Monitor_Back.Entities.Views {
    public class TurmaList {
        public int Id { get; set; }
        public int DiaSemana { get; set; }
        public TimeSpan Horario { get; set; }

        public int? Professor_Id { get; set; }

        public string? Turma_Tipo { get; set; } = string.Empty;
    }
}