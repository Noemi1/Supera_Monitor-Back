namespace Supera_Monitor_Back.Entities.Views {
    public class CalendarioList {
        public int? Aula_Id { get; set; }
        public DateTime Data { get; set; }
        public int Turma_Id { get; set; }
        public string Turma { get; set; } = string.Empty;
        public int CapacidadeMaximaAlunos { get; set; }
        public int? Professor_Id { get; set; }
        public string? Professor { get; set; }
        public string? CorLegenda { get; set; }
        public string? Observacao { get; set; }
        public bool? Finalizada { get; set; }

        public int Turma_Tipo_Id { get; set; }
        public string Turma_Tipo { get; set; } = string.Empty;
    }
}
