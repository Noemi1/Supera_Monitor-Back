namespace Supera_Monitor_Back.Entities {
    public class Turma {
        public int Id { get; set; }
        public int DiaSemana { get; set; }
        public TimeSpan Horario { get; set; }

        public int? Professor_Id { get; set; }

        public int? Turma_Tipo_Id { get; set; }
        public Turma_Tipo? Turma_Tipo { get; set; } = null;
    }
}
