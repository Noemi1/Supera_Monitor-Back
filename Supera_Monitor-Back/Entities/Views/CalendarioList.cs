namespace Supera_Monitor_Back.Entities.Views {
    public partial class CalendarioList {
        public int Aula_Id { get; set; }

        public DateTime Data { get; set; }

        public int? Turma_Id { get; set; }

        public string Turma { get; set; } = null!;

        public int CapacidadeMaximaAlunos { get; set; }

        public int Professor_Id { get; set; }

        public string Professor { get; set; } = null!;

        public string CorLegenda { get; set; } = null!;

        public string? Observacao { get; set; }
    }
}
