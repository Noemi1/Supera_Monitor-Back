namespace Supera_Monitor_Back.Entities.Views {
    public class CalendarioList {
        public int? Aula_Id { get; set; }
        public DateTime Data { get; set; }
        public int Turma_Id { get; set; }
        public string Turma { get; set; }
        public int CapacidadeMaximaAlunos { get; set; }
        public int? Professor_Id { get; set; }
        public string? Professor { get; set; }
        public string? CorLegenda { get; set; }

    }
}
