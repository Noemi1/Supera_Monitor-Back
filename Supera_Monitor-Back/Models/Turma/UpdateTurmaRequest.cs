namespace Supera_Monitor_Back.Models.Turma {
    public class UpdateTurmaRequest {
        public int Id;
        public TimeSpan Horario;
        public int DiaSemana;

        public int? Turma_Tipo_Id;
        public int? Professor_Id;
    }
}
